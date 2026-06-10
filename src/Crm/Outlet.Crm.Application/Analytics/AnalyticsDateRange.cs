using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

/// <summary>
/// Shared date-range resolution for the analytics queries: missing bounds default
/// to the trailing 30 days (inclusive, ending today) and inverted ranges fail.
/// </summary>
internal static class AnalyticsDateRange
{
    private const int DefaultWindowDays = 30;

    internal static Result<(DateOnly From, DateOnly To)> Resolve(DateOnly? from, DateOnly? to, DateOnly today)
    {
        var resolvedTo = to ?? today;
        var resolvedFrom = from ?? resolvedTo.AddDays(-(DefaultWindowDays - 1));

        if (resolvedFrom > resolvedTo)
        {
            return Result.Failure<(DateOnly, DateOnly)>(
                $"Analytics.InvalidRange: 'from' ({resolvedFrom:yyyy-MM-dd}) must not be after 'to' ({resolvedTo:yyyy-MM-dd}).");
        }

        return Result.Success((resolvedFrom, resolvedTo));
    }
}
