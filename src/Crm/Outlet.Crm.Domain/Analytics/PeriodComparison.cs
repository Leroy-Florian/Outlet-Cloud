namespace Outlet.Crm.Domain.Analytics;

public enum TrendDirection
{
    Flat = 0,
    Up = 1,
    Down = 2,
}

/// <summary>
/// A metric over the current trailing window versus the immediately preceding
/// window of the same length. <see cref="PercentChange"/> is null when the
/// previous period is zero (no meaningful baseline), otherwise rounded to one
/// decimal place.
/// </summary>
public sealed record PeriodComparison(
    long CurrentPeriod,
    long PreviousPeriod,
    decimal? PercentChange,
    TrendDirection Direction)
{
    public static PeriodComparison Of(long currentPeriod, long previousPeriod)
    {
        decimal? percentChange = previousPeriod == 0
            ? null
            : Math.Round((currentPeriod - previousPeriod) * 100m / previousPeriod, 1);

        var direction = currentPeriod > previousPeriod
            ? TrendDirection.Up
            : currentPeriod < previousPeriod ? TrendDirection.Down : TrendDirection.Flat;

        return new PeriodComparison(currentPeriod, previousPeriod, percentChange, direction);
    }
}
