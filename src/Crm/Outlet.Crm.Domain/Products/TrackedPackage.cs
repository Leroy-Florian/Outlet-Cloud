using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Products;

public sealed class TrackedPackage(Guid id, PackageRegistry registry, PackageId packageId) : Entity<Guid>(id)
{
    public PackageRegistry Registry { get; } = registry;

    public PackageId PackageId { get; } = packageId;
}
