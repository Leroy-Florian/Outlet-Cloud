using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Alerts;

public sealed record EvaluateAlertsCommand(
    Guid ProductId,
    IReadOnlyList<SnapshotCaptureReport>? CaptureReports = null);

/// <summary>
/// Evaluates the v1 alert rules (<see cref="AlertRules"/>) for one product and
/// persists the alerts that fired. Downloads spike/drop compares today's new
/// downloads per package against the average of the previous 7 days; stars
/// milestones compare the two latest repository snapshots. SnapshotFailure
/// alerts come from the optional capture reports the scheduler hands over
/// (a manual evaluation has no capture context, so it never raises them).
/// De-duplication: at most one alert per product, type and calendar day, so the
/// scheduler can re-evaluate every few hours without spamming the inbox.
/// </summary>
public sealed class EvaluateAlertsUseCase(
    IProductRepository products,
    IDownloadSnapshotRepository downloadSnapshots,
    IRepositorySnapshotRepository repositorySnapshots,
    IAlertRepository alerts,
    ICurrentDateTimeProvider clock)
    : IUseCase<EvaluateAlertsCommand, IReadOnlyList<Alert>>
{
    private const int BaselineDays = 7;

    public async Task<Result<IReadOnlyList<Alert>>> HandleAsync(
        EvaluateAlertsCommand command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyList<Alert>>(ProductErrors.NotFound(productId));
        }

        List<AlertSignal> signals =
        [
            .. await EvaluateDownloadSignalsAsync(productId, cancellationToken),
            .. await EvaluateStarSignalsAsync(productId, cancellationToken),
            .. EvaluateCaptureFailureSignals(command.CaptureReports),
        ];

        var existing = await alerts.ListByProductAsync(productId, cancellationToken);
        var today = clock.Today;

        List<Alert> created = [];
        foreach (var signal in signals)
        {
            var alreadyRaisedToday = existing.Any(a =>
                a.Type == signal.Type && DateOnly.FromDateTime(a.TriggeredAt) == today)
                || created.Any(a => a.Type == signal.Type);
            if (alreadyRaisedToday)
            {
                continue;
            }

            var alert = Alert.Create(productId, signal.Type, signal.Message, clock.UtcNow);
            if (alert.IsFailure)
            {
                return Result.Failure<IReadOnlyList<Alert>>(alert.Error!);
            }

            await alerts.AddAsync(alert.Value!, cancellationToken);
            created.Add(alert.Value!);
        }

        return Result.Success<IReadOnlyList<Alert>>(created);
    }

    private async Task<List<AlertSignal>> EvaluateDownloadSignalsAsync(ProductId productId, CancellationToken cancellationToken)
    {
        var snapshots = await downloadSnapshots.ListByProductAsync(productId, cancellationToken);
        var today = clock.Today;
        var report = DailyDownloads.Compute(snapshots, today.AddDays(-BaselineDays), today);

        List<AlertSignal> signals = [];
        foreach (var source in report.Sources)
        {
            var todayDownloads = source.Days[^1].Downloads;
            var average = (decimal)source.Days.Take(BaselineDays).Sum(d => d.Downloads) / BaselineDays;

            if (AlertRules.EvaluateDownloads($"{source.Registry}:{source.PackageId}", todayDownloads, average) is { } signal)
            {
                signals.Add(signal);
            }
        }

        return signals;
    }

    private async Task<List<AlertSignal>> EvaluateStarSignalsAsync(ProductId productId, CancellationToken cancellationToken)
    {
        var snapshots = await repositorySnapshots.ListByProductAsync(productId, cancellationToken);

        List<AlertSignal> signals = [];
        foreach (var group in snapshots.GroupBy(s => s.Repository.FullName))
        {
            List<RepositorySnapshot> ordered = [.. group.OrderBy(s => s.CapturedAt)];
            if (ordered.Count < 2)
            {
                continue;
            }

            if (AlertRules.EvaluateStars(group.Key, ordered[^2].Stars, ordered[^1].Stars) is { } signal)
            {
                signals.Add(signal);
            }
        }

        return signals;
    }

    private static List<AlertSignal> EvaluateCaptureFailureSignals(IReadOnlyList<SnapshotCaptureReport>? reports)
    {
        if (reports is null)
        {
            return [];
        }

        List<SnapshotCaptureReport> failures = [.. reports.Where(r => !r.Succeeded)];
        if (failures.Count is 0)
        {
            return [];
        }

        var targets = string.Join(", ", failures.Select(f => f.Target));
        return
        [
            new AlertSignal(
                AlertType.SnapshotFailure,
                $"Snapshot capture failed for {failures.Count} target(s): {targets}."),
        ];
    }
}
