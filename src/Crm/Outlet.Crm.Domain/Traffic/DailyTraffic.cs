namespace Outlet.Crm.Domain.Traffic;

public sealed record DailyTrafficPoint(DateOnly Date, long PageViews);

public sealed record TrafficCount(string Key, long Count);

public sealed record DailyTrafficReport(
    DateOnly From,
    DateOnly To,
    long TotalPageViews,
    IReadOnlyList<DailyTrafficPoint> Days,
    IReadOnlyList<TrafficCount> TopPaths,
    IReadOnlyList<TrafficCount> TopReferrers);

/// <summary>
/// Aggregates raw traffic samples into per-day page views plus top paths and
/// top referrer sources over the inclusive [from, to] range (zero-filled days,
/// top lists capped at 10, ties broken by key for determinism).
/// </summary>
public static class DailyTraffic
{
    private const int TopCount = 10;

    public static DailyTrafficReport Compute(IEnumerable<TrafficSample> samples, DateOnly from, DateOnly to)
    {
        List<TrafficSample> inRange = [.. samples.Where(s =>
        {
            var day = DateOnly.FromDateTime(s.OccurredAt);
            return day >= from && day <= to;
        })];

        List<DailyTrafficPoint> days = [];
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var current = day;
            days.Add(new DailyTrafficPoint(current, inRange.LongCount(s => DateOnly.FromDateTime(s.OccurredAt) == current)));
        }

        return new DailyTrafficReport(
            from,
            to,
            inRange.Count,
            days,
            Top(inRange.Select(s => s.Path)),
            Top(inRange.Select(s => s.ReferrerSource)));
    }

    private static IReadOnlyList<TrafficCount> Top(IEnumerable<string> keys) =>
        [.. keys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Select(group => new TrafficCount(group.Key, group.LongCount()))
            .OrderByDescending(count => count.Count)
            .ThenBy(count => count.Key, StringComparer.Ordinal)
            .Take(TopCount)];
}
