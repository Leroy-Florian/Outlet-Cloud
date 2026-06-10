using Outlet.Crm.Application.Alerts;
using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.Ports;

namespace Outlet.Crm.Web.Workers;

/// <summary>
/// HOST-LEVEL scheduler (deliberately outside Domain/Application: timing is an
/// infrastructure concern). Every "Crm:SnapshotIntervalHours" hours (default 24,
/// 0 = disabled) it captures snapshots for every non-archived product through
/// <see cref="CaptureProductSnapshotsUseCase"/>, then evaluates the alert rules
/// with the capture reports. Best-effort all the way down: a failing product,
/// capture or evaluation is logged and never crashes the host.
/// </summary>
public sealed class SnapshotCaptureWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SnapshotCaptureWorker> logger)
    : BackgroundService
{
    private const int DefaultIntervalHours = 24;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = configuration.GetValue("Crm:SnapshotIntervalHours", DefaultIntervalHours);
        if (intervalHours <= 0)
        {
            logger.LogInformation("Scheduled snapshot capture is disabled (Crm:SnapshotIntervalHours = {Hours}).", intervalHours);
            return;
        }

        logger.LogInformation("Scheduled snapshot capture every {Hours}h.", intervalHours);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        try
        {
            do
            {
                await CaptureAllProductsAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal host shutdown.
        }
    }

    private async Task CaptureAllProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var capture = scope.ServiceProvider.GetRequiredService<CaptureProductSnapshotsUseCase>();
            var evaluateAlerts = scope.ServiceProvider.GetRequiredService<EvaluateAlertsUseCase>();

            var active = (await products.ListAsync(cancellationToken)).Where(p => !p.IsArchived);

            foreach (var product in active)
            {
                await CaptureProductAsync(product.Id.Value, product.Name, capture, evaluateAlerts, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scheduled snapshot capture run failed.");
        }
    }

    private async Task CaptureProductAsync(
        Guid productId,
        string productName,
        CaptureProductSnapshotsUseCase capture,
        EvaluateAlertsUseCase evaluateAlerts,
        CancellationToken cancellationToken)
    {
        try
        {
            var reports = await capture.HandleAsync(new CaptureProductSnapshotsCommand(productId), cancellationToken);
            if (reports.IsFailure)
            {
                logger.LogWarning("Snapshot capture failed for product {Product}: {Error}", productName, reports.Error);
                return;
            }

            foreach (var report in reports.Value!)
            {
                if (report.Succeeded)
                {
                    logger.LogInformation("Captured snapshot of {Target} for product {Product}.", report.Target, productName);
                }
                else
                {
                    logger.LogWarning("Snapshot of {Target} failed for product {Product}: {Error}", report.Target, productName, report.Error);
                }
            }

            var alerts = await evaluateAlerts.HandleAsync(new EvaluateAlertsCommand(productId, reports.Value), cancellationToken);
            if (alerts.IsFailure)
            {
                logger.LogWarning("Alert evaluation failed for product {Product}: {Error}", productName, alerts.Error);
            }
            else if (alerts.Value!.Count > 0)
            {
                logger.LogInformation("{Count} alert(s) triggered for product {Product}.", alerts.Value.Count, productName);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Snapshot capture crashed for product {Product}.", productName);
        }
    }
}
