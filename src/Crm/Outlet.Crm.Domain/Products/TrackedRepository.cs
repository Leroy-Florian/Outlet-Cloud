using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Products;

public sealed class TrackedRepository(Guid id, RepositoryName repository) : Entity<Guid>(id)
{
    public RepositoryName Repository { get; } = repository;
}
