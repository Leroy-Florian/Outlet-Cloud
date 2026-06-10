using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class GetProductAnalyticsSummaryUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();
    private readonly FakeDownloadSnapshotRepository _downloads = new();
    private readonly FakeRepositorySnapshotRepository _repositories = new();
    private readonly FakeTrafficSampleRepository _traffic = new();

    private GetProductAnalyticsSummaryUseCase BuildUseCase() =>
        new(_products, _downloads, _repositories, _traffic, new FixedClock(Now));

    private Product Seed()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    [Fact]
    public async Task Should_Fail_When_ProductIsUnknown()
    {
        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_ReturnEmptySummary_When_NoDataExists()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalDownloads.Should().Be(0);
        result.Value.Packages.Should().BeEmpty();
        result.Value.Repositories.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_BuildSummary_When_SnapshotsAndTrafficExist()
    {
        var product = Seed();
        var packageId = PackageId.Create("outlet.cli").Value!;
        _downloads.Items.Add(DownloadSnapshot.Create(product.Id, PackageRegistry.NuGet, packageId, 100, Now.AddDays(-10)).Value!);
        _downloads.Items.Add(DownloadSnapshot.Create(product.Id, PackageRegistry.NuGet, packageId, 160, Now.AddDays(-1)).Value!);
        _repositories.Items.Add(RepositorySnapshot.Create(
            product.Id, RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!, 3, 42, 7, Now).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now.AddDays(-10)).Value!);

        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(product.Id.Value), CancellationToken.None);

        var summary = result.Value!;
        summary.TotalDownloads.Should().Be(160);
        summary.DownloadsLast7Days.Should().Be(60);
        summary.DownloadsLast30Days.Should().Be(60);
        summary.PageViewsLast7Days.Should().Be(1);
        summary.PageViewsLast30Days.Should().Be(2);
        summary.Packages.Should().ContainSingle().Which.TotalDownloads.Should().Be(160);
        var repo = summary.Repositories.Should().ContainSingle().Subject;
        repo.Repository.Should().Be("Leroy-Florian/Outlet-CLI");
        repo.Stars.Should().Be(42);
        repo.OpenIssues.Should().Be(3);
        repo.Forks.Should().Be(7);
    }

    [Fact]
    public async Task Should_OnlyLoadTrafficSinceThirtyDays_When_OlderSamplesExist()
    {
        var product = Seed();
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now.AddDays(-40)).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now).Value!);

        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(product.Id.Value), CancellationToken.None);

        result.Value!.PageViewsLast30Days.Should().Be(1);
    }

    [Fact]
    public async Task Should_DefaultPeriodToThirtyDays_When_DaysIsOmitted()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(product.Id.Value), CancellationToken.None);

        result.Value!.PeriodDays.Should().Be(30);
    }

    [Fact]
    public async Task Should_ClampPeriodDays_When_DaysIsOutOfRange()
    {
        var product = Seed();

        (await BuildUseCase().HandleAsync(new GetProductAnalyticsSummaryQuery(product.Id.Value, 0)))
            .Value!.PeriodDays.Should().Be(1);
        (await BuildUseCase().HandleAsync(new GetProductAnalyticsSummaryQuery(product.Id.Value, 400)))
            .Value!.PeriodDays.Should().Be(365);
    }

    [Fact]
    public async Task Should_CompareAgainstPreviousWindow_When_DaysIsProvided()
    {
        var product = Seed();
        // 20-day window: current = days -19..0, previous = days -39..-20.
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now.AddDays(-40)).Value!); // excluded
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now.AddDays(-39)).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now.AddDays(-20)).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now).Value!);

        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(product.Id.Value, 20), CancellationToken.None);

        var summary = result.Value!;
        summary.PeriodDays.Should().Be(20);
        summary.PageViews.PreviousPeriod.Should().Be(2);
        summary.PageViews.CurrentPeriod.Should().Be(1);
        summary.PageViews.PercentChange.Should().Be(-50.0m);
    }

    [Fact]
    public async Task Should_ReturnNullPercentChange_When_PreviousWindowHasNoDownloads()
    {
        var product = Seed();
        var packageId = PackageId.Create("outlet.cli").Value!;
        _downloads.Items.Add(DownloadSnapshot.Create(product.Id, PackageRegistry.NuGet, packageId, 100, Now.AddDays(-1)).Value!);
        _downloads.Items.Add(DownloadSnapshot.Create(product.Id, PackageRegistry.NuGet, packageId, 150, Now).Value!);

        var result = await BuildUseCase().HandleAsync(
            new GetProductAnalyticsSummaryQuery(product.Id.Value, 7), CancellationToken.None);

        result.Value!.Downloads.CurrentPeriod.Should().Be(50);
        result.Value!.Downloads.PreviousPeriod.Should().Be(0);
        result.Value!.Downloads.PercentChange.Should().BeNull();
    }
}
