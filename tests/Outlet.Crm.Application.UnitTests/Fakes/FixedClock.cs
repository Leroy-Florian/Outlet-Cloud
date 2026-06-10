using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.UnitTests.Fakes;

/// <summary>A deterministic clock for use cases that stamp creation/capture times.</summary>
public sealed class FixedClock(DateTime utcNow) : ICurrentDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
