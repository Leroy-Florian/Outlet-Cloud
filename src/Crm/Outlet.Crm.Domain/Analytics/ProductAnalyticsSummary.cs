using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Domain.Analytics;

public sealed record PackageTotal(PackageRegistry Registry, string PackageId, long TotalDownloads, DateTime CapturedAt);

public sealed record RepositoryTotal(string Repository, int Stars, int OpenIssues, int Forks, DateTime CapturedAt);

public sealed record ProductAnalyticsSummary(
    long TotalDownloads,
    long DownloadsLast7Days,
    long DownloadsLast30Days,
    long PageViewsLast7Days,
    long PageViewsLast30Days,
    int PeriodDays,
    PeriodComparison Downloads,
    PeriodComparison PageViews,
    IReadOnlyList<PackageTotal> Packages,
    IReadOnlyList<RepositoryTotal> Repositories);

/// <summary>
/// One-stop dashboard summary: latest cumulative totals per package and per
/// repository, new downloads and page views over the trailing 7 and 30 days
/// (inclusive windows ending today), plus a comparison of the trailing
/// <c>periodDays</c> window against the immediately preceding window of the
/// same length ("% vs previous period" badges).
/// </summary>
public static class ProductAnalyticsSummaryCalculator
{
    public static ProductAnalyticsSummary Compute(
        IEnumerable<DownloadSnapshot> downloadSnapshots,
        IEnumerable<RepositorySnapshot> repositorySnapshots,
        IEnumerable<TrafficSample> trafficSamples,
        DateOnly today,
        int periodDays)
    {
        List<DownloadSnapshot> downloads = [.. downloadSnapshots];
        List<TrafficSample> traffic = [.. trafficSamples];

        List<PackageTotal> packages = [.. downloads
            .GroupBy(s => (s.Registry, s.PackageId.Value))
            .Select(group =>
            {
                var latest = group.OrderBy(s => s.CapturedAt).Last();
                return new PackageTotal(latest.Registry, latest.PackageId.Value, latest.TotalDownloads, latest.CapturedAt);
            })
            .OrderBy(p => p.Registry)
            .ThenBy(p => p.PackageId, StringComparer.Ordinal)];

        List<RepositoryTotal> repositories = [.. repositorySnapshots
            .GroupBy(s => s.Repository.FullName)
            .Select(group =>
            {
                var latest = group.OrderBy(s => s.CapturedAt).Last();
                return new RepositoryTotal(group.Key, latest.Stars, latest.OpenIssues, latest.Forks, latest.CapturedAt);
            })
            .OrderBy(r => r.Repository, StringComparer.Ordinal)];

        var currentFrom = today.AddDays(-(periodDays - 1));
        var previousTo = currentFrom.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(periodDays - 1));

        return new ProductAnalyticsSummary(
            packages.Sum(p => p.TotalDownloads),
            DailyDownloads.Compute(downloads, today.AddDays(-6), today).TotalDownloads,
            DailyDownloads.Compute(downloads, today.AddDays(-29), today).TotalDownloads,
            DailyTraffic.Compute(traffic, today.AddDays(-6), today).TotalPageViews,
            DailyTraffic.Compute(traffic, today.AddDays(-29), today).TotalPageViews,
            periodDays,
            PeriodComparison.Of(
                DailyDownloads.Compute(downloads, currentFrom, today).TotalDownloads,
                DailyDownloads.Compute(downloads, previousFrom, previousTo).TotalDownloads),
            PeriodComparison.Of(
                DailyTraffic.Compute(traffic, currentFrom, today).TotalPageViews,
                DailyTraffic.Compute(traffic, previousFrom, previousTo).TotalPageViews),
            packages,
            repositories);
    }
}
