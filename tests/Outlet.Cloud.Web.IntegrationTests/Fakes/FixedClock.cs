using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Web.IntegrationTests.Fakes;

public sealed class FixedClock(DateTime utcNow) : ICurrentDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(utcNow);
    public DateTime UtcNow => utcNow;
}
