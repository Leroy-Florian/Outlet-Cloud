using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Traffic;

public sealed record RecordTrafficEventCommand(
    Guid ProductId,
    string Path,
    string? Referrer,
    string? UserAgent,
    DateTime? OccurredAt);

/// <summary>
/// Ingests one docs/site page view; the clock stamps the event when the caller
/// does not supply an occurrence time.
/// </summary>
public sealed class RecordTrafficEventUseCase(
    ITrafficSampleRepository traffic,
    IProductRepository products,
    ICurrentDateTimeProvider clock)
    : IUseCase<RecordTrafficEventCommand>
{
    public async Task<Result> HandleAsync(RecordTrafficEventCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        var sample = TrafficSample.Create(
            productId, command.Path, command.Referrer, command.UserAgent, command.OccurredAt ?? clock.UtcNow);
        if (sample.IsFailure)
        {
            return Result.Failure(sample.Error!);
        }

        await traffic.AddAsync(sample.Value!, cancellationToken);

        return Result.Success();
    }
}
