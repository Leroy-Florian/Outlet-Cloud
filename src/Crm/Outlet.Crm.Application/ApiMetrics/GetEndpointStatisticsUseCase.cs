using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.ApiMetrics;

public sealed record GetEndpointStatisticsQuery(Guid ProductId, DateTime Since);

/// <summary>Aggregates the metric samples of a product into per-endpoint statistics.</summary>
public sealed class GetEndpointStatisticsUseCase(IApiMetricRepository metrics)
    : IUseCase<GetEndpointStatisticsQuery, IReadOnlyList<EndpointStatistics>>
{
    public async Task<Result<IReadOnlyList<EndpointStatistics>>> HandleAsync(
        GetEndpointStatisticsQuery command,
        CancellationToken cancellationToken = default)
    {
        var samples = await metrics.ListSinceAsync(new ProductId(command.ProductId), command.Since, cancellationToken);
        return Result.Success(EndpointStatisticsCalculator.Compute(samples));
    }
}
