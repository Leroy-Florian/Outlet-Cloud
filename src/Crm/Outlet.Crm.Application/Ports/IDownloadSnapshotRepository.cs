using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="DownloadSnapshot"/> aggregates.</summary>
public interface IDownloadSnapshotRepository
{
    Task<IReadOnlyList<DownloadSnapshot>> ListByPackageAsync(
        ProductId productId,
        PackageRegistry registry,
        PackageId packageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadSnapshot>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task AddAsync(DownloadSnapshot snapshot, CancellationToken cancellationToken = default);
}
