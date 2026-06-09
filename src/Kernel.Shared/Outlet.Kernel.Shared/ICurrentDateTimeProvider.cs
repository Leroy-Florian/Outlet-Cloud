namespace Outlet.Kernel.Shared;

public interface ICurrentDateTimeProvider
{
    DateOnly Today { get; }
    DateTime UtcNow { get; }
}
