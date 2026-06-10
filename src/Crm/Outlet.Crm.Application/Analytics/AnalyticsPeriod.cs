namespace Outlet.Crm.Application.Analytics;

/// <summary>
/// Shared resolution of the "?days=N" comparison window: missing values default
/// to 30 days and out-of-range values are clamped into [1, 365].
/// </summary>
internal static class AnalyticsPeriod
{
    private const int DefaultDays = 30;
    private const int MinDays = 1;
    private const int MaxDays = 365;

    internal static int Resolve(int? days) => Math.Clamp(days ?? DefaultDays, MinDays, MaxDays);
}
