using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="ApiMetricSample"/> aggregates.</summary>
public interface IApiMetricRepository
{
    Task<IReadOnlyList<ApiMetricSample>> ListSinceAsync(ProductId productId, DateTime since, CancellationToken cancellationToken = default);

    Task AddAsync(ApiMetricSample sample, CancellationToken cancellationToken = default);
}
