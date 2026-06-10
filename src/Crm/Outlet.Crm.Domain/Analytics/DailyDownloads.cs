namespace Outlet.Crm.Domain.Analytics;

public sealed record DailyDownloadPoint(DateOnly Date, long Downloads);

/// <summary>New downloads of one tracked package over the requested range, day by day.</summary>
public sealed record DownloadSourceBreakdown(
    PackageRegistry Registry,
    string PackageId,
    long Downloads,
    IReadOnlyList<DailyDownloadPoint> Days);

public sealed record DailyDownloadReport(
    DateOnly From,
    DateOnly To,
    long TotalDownloads,
    IReadOnlyList<DailyDownloadPoint> Days,
    IReadOnlyList<DownloadSourceBreakdown> Sources);

/// <summary>
/// Turns cumulative download snapshots into per-day NEW downloads. A snapshot's
/// delta against the previous snapshot of the same package is attributed to the
/// day it was captured; decreasing totals (registry corrections) are clamped to 0.
/// Every day of the inclusive [from, to] range is present, zero-filled.
/// </summary>
public static class DailyDownloads
{
    public static DailyDownloadReport Compute(IEnumerable<DownloadSnapshot> snapshots, DateOnly from, DateOnly to)
    {
        List<DateOnly> allDays = [.. EachDay(from, to)];

        List<DownloadSourceBreakdown> sources = [.. snapshots
            .GroupBy(s => (s.Registry, s.PackageId.Value))
            .Select(group => ComputeSource(group.Key.Registry, group.Key.Value, group, allDays))
            .OrderBy(s => s.Registry)
            .ThenBy(s => s.PackageId, StringComparer.Ordinal)];

        List<DailyDownloadPoint> days = [.. allDays
            .Select((day, index) => new DailyDownloadPoint(day, sources.Sum(s => s.Days[index].Downloads)))];

        return new DailyDownloadReport(from, to, days.Sum(d => d.Downloads), days, sources);
    }

    private static DownloadSourceBreakdown ComputeSource(
        PackageRegistry registry,
        string packageId,
        IEnumerable<DownloadSnapshot> snapshots,
        IReadOnlyList<DateOnly> allDays)
    {
        List<DownloadSnapshot> ordered = [.. snapshots.OrderBy(s => s.CapturedAt)];
        var perDay = new Dictionary<DateOnly, long>();

        for (var i = 1; i < ordered.Count; i++)
        {
            var delta = Math.Max(ordered[i].TotalDownloads - ordered[i - 1].TotalDownloads, 0);
            var day = DateOnly.FromDateTime(ordered[i].CapturedAt);
            perDay[day] = perDay.GetValueOrDefault(day) + delta;
        }

        List<DailyDownloadPoint> days = [.. allDays
            .Select(day => new DailyDownloadPoint(day, perDay.GetValueOrDefault(day)))];

        return new DownloadSourceBreakdown(registry, packageId, days.Sum(d => d.Downloads), days);
    }

    private static IEnumerable<DateOnly> EachDay(DateOnly from, DateOnly to)
    {
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            yield return day;
        }
    }
}
