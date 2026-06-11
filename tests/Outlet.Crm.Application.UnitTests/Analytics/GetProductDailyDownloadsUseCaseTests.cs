using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Releases;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class GetProductDailyDownloadsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly Today = new(2026, 6, 9);

    private readonly FakeProductRepository _products = new();
    private readonly FakeDownloadSnapshotRepository _snapshots = new();
    private readonly FakeReleaseRepository _releases = new();

    private GetProductDailyDownloadsUseCase BuildUseCase() =>
        new(_products, _snapshots, _releases, new FixedClock(Now));

    private Product Seed()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    private void AddSnapshot(ProductId productId, string package, long total, DateTime at, PackageRegistry registry = PackageRegistry.NuGet) =>
        _snapshots.Items.Add(DownloadSnapshot.Create(productId, registry, PackageId.Create(package).Value!, total, at).Value!);

    [Fact]
    public async Task Should_Fail_When_ProductIsUnknown()
    {
        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(Guid.NewGuid(), null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_DefaultToLast30Days_When_NoRangeSupplied()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.To.Should().Be(Today);
        result.Value.From.Should().Be(Today.AddDays(-29));
        result.Value.Days.Should().HaveCount(30);
    }

    [Fact]
    public async Task Should_DefaultFromRelativeToTo_When_OnlyToSupplied()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, null, new DateOnly(2026, 5, 1)), CancellationToken.None);

        result.Value!.To.Should().Be(new DateOnly(2026, 5, 1));
        result.Value.From.Should().Be(new DateOnly(2026, 4, 2));
    }

    [Fact]
    public async Task Should_Fail_When_FromIsAfterTo()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Analytics.InvalidRange: 'from' (2026-06-02) must not be after 'to' (2026-06-01).");
    }

    [Fact]
    public async Task Should_AcceptSingleDayRange_When_FromEqualsTo()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, Today, Today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Days.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_AggregateAcrossSources_When_ProductHasSeveralPackages()
    {
        var product = Seed();
        AddSnapshot(product.Id, "outlet.cli", 100, Now.AddDays(-1));
        AddSnapshot(product.Id, "outlet.cli", 130, Now);
        AddSnapshot(product.Id, "@outlet/web", 10, Now.AddDays(-1), PackageRegistry.Npm);
        AddSnapshot(product.Id, "@outlet/web", 25, Now, PackageRegistry.Npm);
        AddSnapshot(ProductId.New(), "other", 999, Now);

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, Today.AddDays(-1), Today), CancellationToken.None);

        var report = result.Value!;
        report.TotalDownloads.Should().Be(45);
        report.Days.Select(d => d.Downloads).Should().Equal(0, 45);
        report.Sources.Select(s => (s.Registry, s.PackageId, s.Downloads)).Should().Equal(
            (PackageRegistry.NuGet, "outlet.cli", 30L),
            (PackageRegistry.Npm, "@outlet/web", 15L));
    }

    [Fact]
    public async Task Should_ClampNegativeDeltas_When_RegistryTotalsDecrease()
    {
        var product = Seed();
        AddSnapshot(product.Id, "outlet.cli", 100, Now.AddDays(-1));
        AddSnapshot(product.Id, "outlet.cli", 90, Now);

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, Today.AddDays(-1), Today), CancellationToken.None);

        result.Value!.TotalDownloads.Should().Be(0);
        result.Value.Days.Select(d => d.Downloads).Should().Equal(0, 0);
    }

    [Fact]
    public async Task Should_IncludeReleaseMarkersWithinRange_When_ReleasesWereRecorded()
    {
        var product = Seed();
        var repository = RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!;
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v2.0.0", "Two", Now.AddDays(-1)).Value!);
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v1.0.0", null, Now.AddDays(-1)).Value!);
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v0.9.0", null, Now.AddDays(-40)).Value!);
        _releases.Items.Add(ReleaseRecord.Create(ProductId.New(), repository, "v9.9.9", null, Now).Value!);

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, Today.AddDays(-2), Today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Releases.Should().Equal(
            new ReleaseMarker(Today.AddDays(-1), "v1.0.0", "Leroy-Florian/Outlet-CLI"),
            new ReleaseMarker(Today.AddDays(-1), "v2.0.0", "Leroy-Florian/Outlet-CLI"));
    }

    [Fact]
    public async Task Should_IncludeBoundaryReleases_When_PublishedExactlyOnRangeEdges()
    {
        var product = Seed();
        var repository = RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!;
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v1.0.0", null, Now.AddDays(-2)).Value!);
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v2.0.0", null, Now).Value!);
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v0.1.0", null, Now.AddDays(-3)).Value!);
        _releases.Items.Add(ReleaseRecord.Create(product.Id, repository, "v3.0.0", null, Now.AddDays(1)).Value!);

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, Today.AddDays(-2), Today), CancellationToken.None);

        result.Value!.Releases.Select(r => r.TagName).Should().Equal("v1.0.0", "v2.0.0");
    }

    [Fact]
    public async Task Should_ReturnEmptyReleaseMarkers_When_NoReleaseFallsInRange()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetProductDailyDownloadsQuery(product.Id.Value, null, null), CancellationToken.None);

        result.Value!.Releases.Should().BeEmpty();
    }
}
