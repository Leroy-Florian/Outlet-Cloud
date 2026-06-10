using System.Globalization;
using Outlet.Crm.Domain.Objectives;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Objectives;

/// <summary>Parses the wire format of a month ("2026-06") into its first day.</summary>
public static class ObjectiveMonth
{
    public static Result<DateOnly> Parse(string month)
    {
        if (DateOnly.TryParseExact(month?.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return Result.Success(parsed);
        }

        return Result.Failure<DateOnly>(ObjectiveErrors.InvalidMonth);
    }
}
