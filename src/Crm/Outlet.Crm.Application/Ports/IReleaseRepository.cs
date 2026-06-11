using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Releases;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="ReleaseRecord"/> aggregates.</summary>
public interface IReleaseRepository
{
    Task<IReadOnlyList<ReleaseRecord>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(ProductId productId, RepositoryName repository, string tagName, CancellationToken cancellationToken = default);

    Task AddAsync(ReleaseRecord release, CancellationToken cancellationToken = default);
}
