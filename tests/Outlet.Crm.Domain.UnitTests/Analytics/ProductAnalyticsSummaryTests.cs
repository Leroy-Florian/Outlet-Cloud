using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class ProductAnalyticsSummaryTests
{
    private static readonly DateOnly Today = new(2026, 6, 9);

    private static readonly DateTime TodayNoon = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ProductId Product = ProductId.New();

    private static DownloadSnapshot Download(string package, long total, DateTime at, PackageRegistry registry = PackageRegistry.NuGet) =>
        DownloadSnapshot.Create(Product, registry, PackageId.Create(package).Value!, total, at).Value!;

    private static RepositorySnapshot Repo(string fullName, int issues, int stars, int forks, DateTime at) =>
        RepositorySnapshot.Create(Product, RepositoryName.Create(fullName).Value!, issues, stars, forks, at).Value!;

    private static TrafficSample View(DateTime at) =>
        TrafficSample.Create(Product, "/", null, null, at).Value!;

    [Fact]
    public void Should_ReturnZeros_When_NoData()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute([], [], [], Today);

        summary.TotalDownloads.Should().Be(0);
        summary.DownloadsLast7Days.Should().Be(0);
        summary.DownloadsLast30Days.Should().Be(0);
        summary.PageViewsLast7Days.Should().Be(0);
        summary.PageViewsLast30Days.Should().Be(0);
        summary.Packages.Should().BeEmpty();
        summary.Repositories.Should().BeEmpty();
    }

    [Fact]
    public void Should_KeepLatestSnapshotPerPackage_When_SeveralExist()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute(
        [
            Download("outlet.cli", 100, TodayNoon.AddDays(-1)),
            Download("outlet.cli", 150, TodayNoon),
            Download("alpha", 10, TodayNoon.AddDays(-2), PackageRegistry.Npm),
        ], [], [], Today);

        summary.Packages.Should().HaveCount(2);
        summary.Packages.Select(p => (p.Registry, p.PackageId, p.TotalDownloads)).Should().Equal(
            (PackageRegistry.NuGet, "outlet.cli", 150L),
            (PackageRegistry.Npm, "alpha", 10L));
        summary.Packages[0].CapturedAt.Should().Be(TodayNoon);
        summary.TotalDownloads.Should().Be(160);
    }

    [Fact]
    public void Should_CountDownloadsInTrailingWindows_When_DeltasLandOnBoundaries()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute(
        [
            Download("outlet.cli", 100, TodayNoon.AddDays(-31)),
            Download("outlet.cli", 105, TodayNoon.AddDays(-30)),
            Download("outlet.cli", 115, TodayNoon.AddDays(-29)),
            Download("outlet.cli", 135, TodayNoon.AddDays(-7)),
            Download("outlet.cli", 175, TodayNoon.AddDays(-6)),
            Download("outlet.cli", 255, TodayNoon),
        ], [], [], Today);

        summary.DownloadsLast7Days.Should().Be(120);
        summary.DownloadsLast30Days.Should().Be(150);
    }

    [Fact]
    public void Should_KeepLatestSnapshotPerRepository_When_SeveralExist()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute(
            [],
            [
                Repo("o/b", 5, 50, 3, TodayNoon.AddDays(-1)),
                Repo("o/b", 4, 60, 5, TodayNoon),
                Repo("o/a", 1, 2, 0, TodayNoon.AddDays(-3)),
            ],
            [],
            Today);

        summary.Repositories.Select(r => (r.Repository, r.Stars, r.OpenIssues, r.Forks)).Should().Equal(
            ("o/a", 2, 1, 0),
            ("o/b", 60, 4, 5));
        summary.Repositories[1].CapturedAt.Should().Be(TodayNoon);
    }

    [Fact]
    public void Should_CountPageViewsInTrailingWindows_When_ViewsLandOnBoundaries()
    {
        var summary = ProductAnalyticsSummaryCalculator.Compute(
            [], [],
            [
                View(TodayNoon.AddDays(-30)),
                View(TodayNoon.AddDays(-29)),
                View(TodayNoon.AddDays(-7)),
                View(TodayNoon.AddDays(-6)),
                View(TodayNoon),
            ],
            Today);

        summary.PageViewsLast7Days.Should().Be(2);
        summary.PageViewsLast30Days.Should().Be(4);
    }
}
