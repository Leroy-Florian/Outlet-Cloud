using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.UnitTests.Fakes;

/// <summary>A deterministic clock for use cases that anchor trials on the server time.</summary>
public sealed class FixedClock(DateOnly today) : ICurrentDateTimeProvider
{
    public DateOnly Today { get; } = today;
    public DateTime UtcNow => Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
