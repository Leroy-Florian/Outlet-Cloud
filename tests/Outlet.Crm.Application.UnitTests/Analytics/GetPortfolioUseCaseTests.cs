using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class GetPortfolioUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();
    private readonly FakeDownloadSnapshotRepository _downloads = new();
    private readonly FakeRepositorySnapshotRepository _repositories = new();
    private readonly FakeTrafficSampleRepository _traffic = new();
    private readonly FakeFeedbackRepository _feedback = new();
    private readonly FixedClock _clock = new(Now);

    private GetPortfolioUseCase CreateUseCase() =>
        new(_products, _downloads, _repositories, _traffic, _feedback, _clock);

    private Product AddProduct(string name = "Outlet")
    {
        var product = Product.Create(name, null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    private void AddDownload(ProductId productId, long total, DateTime at) =>
        _downloads.Items.Add(DownloadSnapshot.Create(
            productId, PackageRegistry.NuGet, PackageId.Create("outlet.cli").Value!, total, at).Value!);

    [Fact]
    public async Task Should_ReturnEmptyPortfolio_When_NoProducts()
    {
        var result = await CreateUseCase().HandleAsync(new GetPortfolioQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.PeriodDays.Should().Be(30);
        result.Value!.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ClampPeriodDays_When_OutOfRange()
    {
        (await CreateUseCase().HandleAsync(new GetPortfolioQuery(0))).Value!.PeriodDays.Should().Be(1);
        (await CreateUseCase().HandleAsync(new GetPortfolioQuery(400))).Value!.PeriodDays.Should().Be(365);
        (await CreateUseCase().HandleAsync(new GetPortfolioQuery(7))).Value!.PeriodDays.Should().Be(7);
    }

    [Fact]
    public async Task Should_SkipArchivedProducts_When_BuildingThePortfolio()
    {
        AddProduct("Active");
        var archived = AddProduct("Archived");
        archived.Archive();

        var result = await CreateUseCase().HandleAsync(new GetPortfolioQuery());

        result.Value!.Products.Select(p => p.Name).Should().Equal("Active");
    }

    [Fact]
    public async Task Should_AggregatePerProduct_When_DataExists()
    {
        var product = AddProduct();
        product.TrackPackage(PackageRegistry.NuGet, PackageId.Create("outlet.cli").Value!);
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("outlet-cli").Value!);

        // 7-day window: current = days -6..0, previous = days -13..-7.
        AddDownload(product.Id, 100, Now.AddDays(-14));
        AddDownload(product.Id, 110, Now.AddDays(-13));
        AddDownload(product.Id, 120, Now.AddDays(-7));
        AddDownload(product.Id, 160, Now);

        _repositories.Items.Add(RepositorySnapshot.Create(
            product.Id, RepositoryName.Create("o/r").Value!, 1, 10, 0, Now.AddDays(-1)).Value!);
        _repositories.Items.Add(RepositorySnapshot.Create(
            product.Id, RepositoryName.Create("o/r").Value!, 1, 42, 0, Now).Value!);

        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now.AddDays(-8)).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now).Value!);
        _traffic.Items.Add(TrafficSample.Create(product.Id, "/", null, null, Now).Value!);

        var open = Domain.Feedback.Feedback.Create(product.Id, FeedbackCategory.Bug, "open", null, null, Now).Value!;
        var triaged = Domain.Feedback.Feedback.Create(product.Id, FeedbackCategory.Bug, "triaged", null, null, Now).Value!;
        triaged.Triage();
        var resolved = Domain.Feedback.Feedback.Create(product.Id, FeedbackCategory.Bug, "done", null, null, Now).Value!;
        resolved.Resolve();
        var dismissed = Domain.Feedback.Feedback.Create(product.Id, FeedbackCategory.Bug, "noise", null, null, Now).Value!;
        dismissed.Dismiss();
        _feedback.Items.AddRange([open, triaged, resolved, dismissed]);

        var result = await CreateUseCase().HandleAsync(new GetPortfolioQuery(7));

        var row = result.Value!.Products.Single();
        row.ProductId.Should().Be(product.Id.Value);
        row.Name.Should().Be("Outlet");
        row.PackageCount.Should().Be(2);
        row.TotalDownloads.Should().Be(160);
        row.LatestStars.Should().Be(42);
        row.OpenFeedbackCount.Should().Be(2);
        row.Downloads.CurrentPeriod.Should().Be(40);
        row.Downloads.PreviousPeriod.Should().Be(20);
        row.Downloads.PercentChange.Should().Be(100.0m);
        row.PageViews.CurrentPeriod.Should().Be(2);
        row.PageViews.PreviousPeriod.Should().Be(1);
        row.PageViews.PercentChange.Should().Be(100.0m);
    }

    [Fact]
    public async Task Should_NotMixProducts_When_SeveralExist()
    {
        var first = AddProduct("First");
        var second = AddProduct("Second");
        AddDownload(first.Id, 50, Now);
        _feedback.Items.Add(Domain.Feedback.Feedback.Create(second.Id, FeedbackCategory.Bug, "x", null, null, Now).Value!);

        var result = await CreateUseCase().HandleAsync(new GetPortfolioQuery());

        var rows = result.Value!.Products;
        rows.Single(p => p.Name == "First").OpenFeedbackCount.Should().Be(0);
        rows.Single(p => p.Name == "First").TotalDownloads.Should().Be(50);
        rows.Single(p => p.Name == "Second").OpenFeedbackCount.Should().Be(1);
        rows.Single(p => p.Name == "Second").TotalDownloads.Should().Be(0);
    }
}
