using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetProductAnalyticsSummaryQuery(Guid ProductId);

/// <summary>
/// Dashboard summary: latest cumulative totals per package/repository plus
/// downloads and page views over the trailing 7 and 30 days.
/// </summary>
public sealed class GetProductAnalyticsSummaryUseCase(
    IProductRepository products,
    IDownloadSnapshotRepository downloadSnapshots,
    IRepositorySnapshotRepository repositorySnapshots,
    ITrafficSampleRepository traffic,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetProductAnalyticsSummaryQuery, ProductAnalyticsSummary>
{
    public async Task<Result<ProductAnalyticsSummary>> HandleAsync(
        GetProductAnalyticsSummaryQuery command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<ProductAnalyticsSummary>(ProductErrors.NotFound(productId));
        }

        var today = clock.Today;
        var downloads = await downloadSnapshots.ListByProductAsync(productId, cancellationToken);
        var repositories = await repositorySnapshots.ListByProductAsync(productId, cancellationToken);
        var samples = await traffic.ListSinceAsync(
            productId, today.AddDays(-29).ToDateTime(TimeOnly.MinValue), cancellationToken);

        return Result.Success(ProductAnalyticsSummaryCalculator.Compute(downloads, repositories, samples, today));
    }
}
