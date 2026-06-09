using Outlet.Kernel.Shared;

namespace Outlet.Identity.Application.UnitTests.Fakes;

public sealed class FixedClock(DateTime utcNow) : ICurrentDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(utcNow);
    public DateTime UtcNow => utcNow;
}
