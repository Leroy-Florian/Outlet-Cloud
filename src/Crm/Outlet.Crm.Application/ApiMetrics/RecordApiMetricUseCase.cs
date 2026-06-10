using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.ApiMetrics;

public sealed record RecordApiMetricCommand(Guid ProductId, string Endpoint, int StatusCode, double DurationMs);

/// <summary>Stores one API metric sample (endpoint, status, duration) for a product.</summary>
public sealed class RecordApiMetricUseCase(
    IApiMetricRepository metrics,
    IProductRepository products,
    ICurrentDateTimeProvider clock)
    : IUseCase<RecordApiMetricCommand>
{
    public async Task<Result> HandleAsync(RecordApiMetricCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        var sample = ApiMetricSample.Create(productId, command.Endpoint, command.StatusCode, command.DurationMs, clock.UtcNow);
        if (sample.IsFailure)
        {
            return Result.Failure(sample.Error!);
        }

        await metrics.AddAsync(sample.Value!, cancellationToken);

        return Result.Success();
    }
}
