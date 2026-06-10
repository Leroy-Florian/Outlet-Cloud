using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetProductDailyDownloadsQuery(Guid ProductId, DateOnly? From, DateOnly? To);

/// <summary>
/// Per-day NEW downloads of a product (deltas between cumulative snapshots,
/// clamped at zero), broken down by source (registry + package).
/// </summary>
public sealed class GetProductDailyDownloadsUseCase(
    IProductRepository products,
    IDownloadSnapshotRepository snapshots,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetProductDailyDownloadsQuery, DailyDownloadReport>
{
    public async Task<Result<DailyDownloadReport>> HandleAsync(
        GetProductDailyDownloadsQuery command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<DailyDownloadReport>(ProductErrors.NotFound(productId));
        }

        var range = AnalyticsDateRange.Resolve(command.From, command.To, clock.Today);
        if (range.IsFailure)
        {
            return Result.Failure<DailyDownloadReport>(range.Error!);
        }

        var history = await snapshots.ListByProductAsync(productId, cancellationToken);

        return Result.Success(DailyDownloads.Compute(history, range.Value.From, range.Value.To));
    }
}
