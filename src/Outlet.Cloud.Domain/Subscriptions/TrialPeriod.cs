using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>
/// VALUE OBJECT — the window of a frictionless Pro trial. Dates are passed in by the
/// application layer (via <c>ICurrentDateTimeProvider</c>); the domain never reads the
/// clock itself, so trial maths stay deterministic and unit-testable.
/// </summary>
public sealed class TrialPeriod : ValueObject
{
    public DateOnly StartedOn { get; }
    public DateOnly EndsOn { get; }

    private TrialPeriod(DateOnly startedOn, DateOnly endsOn)
    {
        StartedOn = startedOn;
        EndsOn = endsOn;
    }

    /// <summary>Starts a trial of <paramref name="durationInDays"/> days on <paramref name="startedOn"/>.</summary>
    public static TrialPeriod Of(DateOnly startedOn, int durationInDays)
    {
        if (durationInDays <= 0)
            throw new ArgumentException("Trial duration must be a positive number of days.", nameof(durationInDays));

        return new TrialPeriod(startedOn, startedOn.AddDays(durationInDays));
    }

    /// <summary>Rehydrates a trial window from trusted persistence.</summary>
    public static TrialPeriod Between(DateOnly startedOn, DateOnly endsOn)
    {
        if (endsOn <= startedOn)
            throw new ArgumentException("A trial must end after it starts.", nameof(endsOn));

        return new TrialPeriod(startedOn, endsOn);
    }

    /// <summary>True once <paramref name="today"/> reaches or passes the end date.</summary>
    public bool HasElapsedAsOf(DateOnly today) => today >= EndsOn;

    /// <summary>Whole days left before expiry (0 once elapsed). Drives the "9 days left" CLI banner.</summary>
    public int DaysRemainingAsOf(DateOnly today) =>
        today >= EndsOn ? 0 : EndsOn.DayNumber - today.DayNumber;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartedOn;
        yield return EndsOn;
    }
}
