using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="TrafficSample"/> aggregates.</summary>
public interface ITrafficSampleRepository
{
    Task<IReadOnlyList<TrafficSample>> ListSinceAsync(ProductId productId, DateTime since, CancellationToken cancellationToken = default);

    Task AddAsync(TrafficSample sample, CancellationToken cancellationToken = default);
}
