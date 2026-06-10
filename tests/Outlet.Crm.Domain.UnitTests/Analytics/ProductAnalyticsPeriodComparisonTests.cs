using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class ProductAnalyticsPeriodComparisonTests
{
    private static readonly DateOnly Today = new(2026, 6, 9);

    private static readonly DateTime TodayNoon = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ProductId Product = ProductId.New();

    private static DownloadSnapshot Download(long total, DateTime at) =>
        DownloadSnapshot.Create(Product, PackageRegistry.NuGet, PackageId.Create("outlet.cli").Value!, total, at).Value!;

    private static TrafficSample View(DateTime at) =>
        TrafficSample.Create(Product, "/", null, null, at).Value!;

    [Fact]
    public void Should_ExposePeriodDays_When_Computed()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute([], [], [], Today, 7);

        summary.PeriodDays.Should().Be(7);
        summary.Downloads.Should().Be(PeriodComparison.Of(0, 0));
        summary.PageViews.Should().Be(PeriodComparison.Of(0, 0));
    }

    [Fact]
    public void Should_SplitDownloadsOnTheWindowBoundary_When_ComparingPeriods()
    {
        // 7-day window: current = days -6..0, previous = days -13..-7.
        var summary = ProductAnalyticsSummaryCalculator.Compute(
        [
            Download(100, TodayNoon.AddDays(-15)),
            Download(110, TodayNoon.AddDays(-14)), // before previous window — excluded
            Download(130, TodayNoon.AddDays(-13)), // first day of previous window
            Download(135, TodayNoon.AddDays(-7)),  // last day of previous window
            Download(175, TodayNoon.AddDays(-6)),  // first day of current window
            Download(190, TodayNoon),              // today
        ], [], [], Today, 7);

        summary.Downloads.PreviousPeriod.Should().Be(25);
        summary.Downloads.CurrentPeriod.Should().Be(55);
        summary.Downloads.PercentChange.Should().Be(120.0m);
        summary.Downloads.Direction.Should().Be(TrendDirection.Up);
    }

    [Fact]
    public void Should_SplitPageViewsOnTheWindowBoundary_When_ComparingPeriods()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute(
            [], [],
            [
                View(TodayNoon.AddDays(-14)), // excluded
                View(TodayNoon.AddDays(-13)),
                View(TodayNoon.AddDays(-7)),
                View(TodayNoon.AddDays(-7)),
                View(TodayNoon.AddDays(-6)),
                View(TodayNoon),
            ],
            Today, 7);

        summary.PageViews.PreviousPeriod.Should().Be(3);
        summary.PageViews.CurrentPeriod.Should().Be(2);
        summary.PageViews.PercentChange.Should().Be(-33.3m);
        summary.PageViews.Direction.Should().Be(TrendDirection.Down);
    }

    [Fact]
    public void Should_ReturnNullPercentChange_When_PreviousWindowIsEmpty()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute(
            [], [], [View(TodayNoon)], Today, 7);

        summary.PageViews.CurrentPeriod.Should().Be(1);
        summary.PageViews.PreviousPeriod.Should().Be(0);
        summary.PageViews.PercentChange.Should().BeNull();
    }
}
