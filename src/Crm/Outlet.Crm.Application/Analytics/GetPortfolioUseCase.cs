using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Feedback;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetPortfolioQuery(int? Days = null);

public sealed record PortfolioProductSummary(
    Guid ProductId,
    string Name,
    int PackageCount,
    long TotalDownloads,
    int LatestStars,
    int OpenFeedbackCount,
    PeriodComparison Downloads,
    PeriodComparison PageViews);

public sealed record PortfolioSummary(int PeriodDays, IReadOnlyList<PortfolioProductSummary> Products);

/// <summary>
/// Cross-product dashboard: one row per non-archived product with its latest
/// cumulative totals, current-vs-previous window comparison for downloads and
/// page views, latest stars (summed over tracked repositories) and the number
/// of open (New or Triaged) feedback items. Window defaults to 30 days,
/// clamped to [1, 365]. Goes exclusively through the existing ports.
/// </summary>
public sealed class GetPortfolioUseCase(
    IProductRepository products,
    IDownloadSnapshotRepository downloadSnapshots,
    IRepositorySnapshotRepository repositorySnapshots,
    ITrafficSampleRepository traffic,
    IFeedbackRepository feedbackItems,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetPortfolioQuery, PortfolioSummary>
{
    public async Task<Result<PortfolioSummary>> HandleAsync(GetPortfolioQuery command, CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var periodDays = AnalyticsPeriod.Resolve(command.Days);
        var trafficSince = today.AddDays(-(periodDays * 2 - 1)).ToDateTime(TimeOnly.MinValue);

        List<PortfolioProductSummary> rows = [];
        foreach (var product in (await products.ListAsync(cancellationToken)).Where(p => !p.IsArchived))
        {
            var downloads = await downloadSnapshots.ListByProductAsync(product.Id, cancellationToken);
            var repositories = await repositorySnapshots.ListByProductAsync(product.Id, cancellationToken);
            var samples = await traffic.ListSinceAsync(product.Id, trafficSince, cancellationToken);
            var feedback = await feedbackItems.ListByProductAsync(product.Id, cancellationToken);

            var summary = ProductAnalyticsSummaryCalculator.Compute(downloads, repositories, samples, today, periodDays);

            rows.Add(new PortfolioProductSummary(
                product.Id.Value,
                product.Name,
                product.Packages.Count,
                summary.TotalDownloads,
                summary.Repositories.Sum(r => r.Stars),
                feedback.Count(f => f.Status is FeedbackStatus.New or FeedbackStatus.Triaged),
                summary.Downloads,
                summary.PageViews));
        }

        return Result.Success(new PortfolioSummary(periodDays, rows));
    }
}
