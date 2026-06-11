using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetProductDailyDownloadsQuery(Guid ProductId, DateOnly? From, DateOnly? To);

/// <summary>A release published within the requested range — a vertical marker on the chart.</summary>
public sealed record ReleaseMarker(DateOnly Date, string TagName, string Repository);

/// <summary>The daily download report plus the releases published within the range.</summary>
public sealed record ProductDailyDownloads(
    DateOnly From,
    DateOnly To,
    long TotalDownloads,
    IReadOnlyList<DailyDownloadPoint> Days,
    IReadOnlyList<DownloadSourceBreakdown> Sources,
    IReadOnlyList<ReleaseMarker> Releases);

/// <summary>
/// Per-day NEW downloads of a product (deltas between cumulative snapshots,
/// clamped at zero), broken down by source (registry + package), annotated
/// with the releases published over the range.
/// </summary>
public sealed class GetProductDailyDownloadsUseCase(
    IProductRepository products,
    IDownloadSnapshotRepository snapshots,
    IReleaseRepository releases,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetProductDailyDownloadsQuery, ProductDailyDownloads>
{
    public async Task<Result<ProductDailyDownloads>> HandleAsync(
        GetProductDailyDownloadsQuery command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<ProductDailyDownloads>(ProductErrors.NotFound(productId));
        }

        var range = AnalyticsDateRange.Resolve(command.From, command.To, clock.Today);
        if (range.IsFailure)
        {
            return Result.Failure<ProductDailyDownloads>(range.Error!);
        }

        var history = await snapshots.ListByProductAsync(productId, cancellationToken);
        var report = DailyDownloads.Compute(history, range.Value.From, range.Value.To);

        var published = await releases.ListByProductAsync(productId, cancellationToken);
        List<ReleaseMarker> markers = [.. published
            .Select(r => (Release: r, Date: DateOnly.FromDateTime(r.PublishedAt)))
            .Where(r => r.Date >= range.Value.From && r.Date <= range.Value.To)
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Release.TagName, StringComparer.Ordinal)
            .Select(r => new ReleaseMarker(r.Date, r.Release.TagName, r.Release.Repository.FullName))];

        return Result.Success(new ProductDailyDownloads(
            report.From, report.To, report.TotalDownloads, report.Days, report.Sources, markers));
    }
}
