namespace Outlet.Kernel.Shared;

public sealed class UtcDateTimeProvider : ICurrentDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTime UtcNow => DateTime.UtcNow;
}
