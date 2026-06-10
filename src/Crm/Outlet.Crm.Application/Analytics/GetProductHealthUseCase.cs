using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetProductHealthQuery(Guid ProductId);

/// <summary>
/// Assembles the health-score inputs from STORED data only (no live registry
/// calls in v1) and delegates the math to <see cref="HealthScore"/>:
/// release freshness from version-change markers between download snapshots,
/// download trend from the 30d-vs-previous-30d comparison, repo activity from
/// the latest repository snapshot against the one ~30 days before, snapshot
/// reliability from SnapshotFailure alerts over the trailing week.
/// </summary>
public sealed class GetProductHealthUseCase(
    IProductRepository products,
    IDownloadSnapshotRepository downloadSnapshots,
    IRepositorySnapshotRepository repositorySnapshots,
    IAlertRepository alerts,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetProductHealthQuery, ProductHealth>
{
    private const int TrendWindowDays = 30;

    public async Task<Result<ProductHealth>> HandleAsync(
        GetProductHealthQuery command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<ProductHealth>(ProductErrors.NotFound(productId));
        }

        var today = clock.Today;
        var downloads = await downloadSnapshots.ListByProductAsync(productId, cancellationToken);
        var repositories = await repositorySnapshots.ListByProductAsync(productId, cancellationToken);
        var productAlerts = await alerts.ListByProductAsync(productId, cancellationToken);

        var inputs = new HealthInputs(
            DaysSinceLatestRelease(downloads, today),
            DownloadsPercentChange(downloads, today),
            RepoGrowthPercent(repositories, s => s.OpenIssues),
            RepoGrowthPercent(repositories, s => s.Stars),
            CountRecentCaptureFailures(productAlerts));

        return Result.Success(HealthScore.Compute(inputs));
    }

    private static int? DaysSinceLatestRelease(IReadOnlyList<DownloadSnapshot> downloads, DateOnly today)
    {
        if (HealthScore.LatestObservedRelease(downloads) is not { } releasedAt)
        {
            return null;
        }

        return Math.Max(today.DayNumber - DateOnly.FromDateTime(releasedAt).DayNumber, 0);
    }

    private static decimal? DownloadsPercentChange(IReadOnlyList<DownloadSnapshot> downloads, DateOnly today)
    {
        var currentFrom = today.AddDays(-(TrendWindowDays - 1));
        var previousTo = currentFrom.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(TrendWindowDays - 1));

        return PeriodComparison.Of(
            DailyDownloads.Compute(downloads, currentFrom, today).TotalDownloads,
            DailyDownloads.Compute(downloads, previousFrom, previousTo).TotalDownloads).PercentChange;
    }

    /// <summary>
    /// Growth of one repository counter, summed across tracked repositories: the
    /// latest snapshot against the last snapshot at least <see cref="TrendWindowDays"/>
    /// days older (falling back to the oldest one). Null when no repository has
    /// two snapshots to compare.
    /// </summary>
    private static decimal? RepoGrowthPercent(
        IReadOnlyList<RepositorySnapshot> snapshots,
        Func<RepositorySnapshot, int> counter)
    {
        var baselineTotal = 0;
        var latestTotal = 0;
        var comparable = false;

        foreach (var group in snapshots.GroupBy(s => s.Repository.FullName))
        {
            List<RepositorySnapshot> ordered = [.. group.OrderBy(s => s.CapturedAt)];
            if (ordered.Count < 2)
            {
                continue;
            }

            var latest = ordered[^1];
            var cutoff = latest.CapturedAt.AddDays(-TrendWindowDays);
            var baseline = ordered.LastOrDefault(s => s != latest && s.CapturedAt <= cutoff) ?? ordered[0];

            baselineTotal += counter(baseline);
            latestTotal += counter(latest);
            comparable = true;
        }

        return comparable ? HealthScore.GrowthPercent(baselineTotal, latestTotal) : null;
    }

    private int CountRecentCaptureFailures(IReadOnlyList<Alert> productAlerts)
    {
        var since = clock.UtcNow.AddDays(-HealthScore.ReliabilityWindowDays);
        return productAlerts.Count(a => a.Type is AlertType.SnapshotFailure && a.TriggeredAt >= since);
    }
}
