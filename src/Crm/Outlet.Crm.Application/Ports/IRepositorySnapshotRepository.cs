using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="RepositorySnapshot"/> aggregates.</summary>
public interface IRepositorySnapshotRepository
{
    Task<IReadOnlyList<RepositorySnapshot>> ListByRepositoryAsync(
        ProductId productId,
        RepositoryName repository,
        CancellationToken cancellationToken = default);

    Task AddAsync(RepositorySnapshot snapshot, CancellationToken cancellationToken = default);
}
