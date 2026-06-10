using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Objectives;

public sealed record GetObjectivesProgressQuery(string? Month = null);

public sealed record ObjectiveProgress(
    Guid Id,
    Guid? ProductId,
    ObjectiveMetric Metric,
    DateOnly Month,
    decimal TargetValue,
    decimal ActualValue,
    decimal ProgressPercent);

public sealed record ObjectivesProgressReport(DateOnly Month, IReadOnlyList<ObjectiveProgress> Objectives);

/// <summary>
/// Each objective of the month (default: current) with its actual value from
/// stored data — downloads delta of the month, page views of the month, settled
/// revenue created in the month (amounts summed across currencies, v1
/// approximation), prospects created in the month — and the RAW progress
/// percentage (the frontend caps the displayed value).
/// </summary>
public sealed class GetObjectivesProgressUseCase(
    IObjectiveRepository objectives,
    IProductRepository products,
    IDownloadSnapshotRepository downloadSnapshots,
    ITrafficSampleRepository traffic,
    IPaymentRepository payments,
    IProspectRepository prospects,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetObjectivesProgressQuery, ObjectivesProgressReport>
{
    public async Task<Result<ObjectivesProgressReport>> HandleAsync(
        GetObjectivesProgressQuery command,
        CancellationToken cancellationToken = default)
    {
        DateOnly month;
        if (command.Month is null)
        {
            var today = clock.Today;
            month = new DateOnly(today.Year, today.Month, 1);
        }
        else
        {
            var parsed = ObjectiveMonth.Parse(command.Month);
            if (parsed.IsFailure)
            {
                return Result.Failure<ObjectivesProgressReport>(parsed.Error!);
            }

            month = parsed.Value;
        }

        var monthEnd = month.AddMonths(1).AddDays(-1);
        var items = await objectives.ListByMonthAsync(month, cancellationToken);

        List<ObjectiveProgress> progress = [];
        foreach (var objective in items.OrderBy(o => o.Metric).ThenBy(o => o.ProductId?.Value))
        {
            var scope = await ResolveScopeAsync(objective.ProductId, cancellationToken);
            var actual = await ComputeActualAsync(objective, scope, month, monthEnd, cancellationToken);

            progress.Add(new ObjectiveProgress(
                objective.Id.Value,
                objective.ProductId?.Value,
                objective.Metric,
                objective.Month,
                objective.TargetValue,
                actual,
                objective.ProgressPercent(actual)));
        }

        return Result.Success(new ObjectivesProgressReport(month, progress));
    }

    /// <summary>Product-scoped objective → that product; global objective → every product.</summary>
    private async Task<IReadOnlyList<ProductId>> ResolveScopeAsync(ProductId? productId, CancellationToken cancellationToken)
    {
        if (productId is { } scoped)
        {
            return [scoped];
        }

        return [.. (await products.ListAsync(cancellationToken)).Select(p => p.Id)];
    }

    private async Task<decimal> ComputeActualAsync(
        Objective objective,
        IReadOnlyList<ProductId> scope,
        DateOnly month,
        DateOnly monthEnd,
        CancellationToken cancellationToken)
    {
        switch (objective.Metric)
        {
            case ObjectiveMetric.Downloads:
            {
                var total = 0L;
                foreach (var productId in scope)
                {
                    var snapshots = await downloadSnapshots.ListByProductAsync(productId, cancellationToken);
                    total += DailyDownloads.Compute(snapshots, month, monthEnd).TotalDownloads;
                }

                return total;
            }

            case ObjectiveMetric.PageViews:
            {
                var total = 0L;
                var monthStart = month.ToDateTime(TimeOnly.MinValue);
                foreach (var productId in scope)
                {
                    var samples = await traffic.ListSinceAsync(productId, monthStart, cancellationToken);
                    total += samples.Count(s => DateOnly.FromDateTime(s.OccurredAt) <= monthEnd);
                }

                return total;
            }

            case ObjectiveMetric.Revenue:
            {
                var all = await payments.ListAsync(cancellationToken);
                return all
                    .Where(p => p.Status is PaymentStatus.Settled
                        && InMonth(p.CreatedAt, month, monthEnd)
                        && (objective.ProductId is null || p.ProductId == objective.ProductId))
                    .Sum(p => p.Amount.Amount);
            }

            default:
            {
                var all = await prospects.ListAsync(cancellationToken);
                return all.Count(p => InMonth(p.CreatedAt, month, monthEnd)
                    && (objective.ProductId is null || p.ProductId == objective.ProductId));
            }
        }
    }

    private static bool InMonth(DateTime moment, DateOnly month, DateOnly monthEnd)
    {
        var day = DateOnly.FromDateTime(moment);
        return day >= month && day <= monthEnd;
    }
}
