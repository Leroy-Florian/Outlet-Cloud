namespace Outlet.Crm.Domain.ApiMetrics;

public sealed record EndpointStatistics(string Endpoint, int RequestCount, int ErrorCount, double AverageDurationMs, double P95DurationMs);

public static class EndpointStatisticsCalculator
{
    public static IReadOnlyList<EndpointStatistics> Compute(IEnumerable<ApiMetricSample> samples) =>
        [.. samples
            .GroupBy(s => s.Endpoint)
            .Select(group =>
            {
                List<double> durations = [.. group.Select(s => s.DurationMs).OrderBy(d => d)];
                var p95Index = (int)Math.Ceiling(durations.Count * 0.95) - 1;

                return new EndpointStatistics(
                    group.Key,
                    durations.Count,
                    group.Count(s => s.StatusCode >= 500),
                    durations.Average(),
                    durations[Math.Max(p95Index, 0)]);
            })
            .OrderBy(s => s.Endpoint)];
}
