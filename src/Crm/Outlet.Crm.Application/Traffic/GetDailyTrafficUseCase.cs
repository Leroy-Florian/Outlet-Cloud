using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Traffic;

public sealed record GetDailyTrafficQuery(Guid ProductId, DateOnly? From, DateOnly? To);

/// <summary>Daily page views of a product plus its top paths and referrer sources.</summary>
public sealed class GetDailyTrafficUseCase(
    IProductRepository products,
    ITrafficSampleRepository traffic,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetDailyTrafficQuery, DailyTrafficReport>
{
    public async Task<Result<DailyTrafficReport>> HandleAsync(
        GetDailyTrafficQuery command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<DailyTrafficReport>(ProductErrors.NotFound(productId));
        }

        var range = AnalyticsDateRange.Resolve(command.From, command.To, clock.Today);
        if (range.IsFailure)
        {
            return Result.Failure<DailyTrafficReport>(range.Error!);
        }

        var samples = await traffic.ListSinceAsync(
            productId, range.Value.From.ToDateTime(TimeOnly.MinValue), cancellationToken);

        return Result.Success(DailyTraffic.Compute(samples, range.Value.From, range.Value.To));
    }
}
