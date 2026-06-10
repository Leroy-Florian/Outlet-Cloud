using Outlet.Crm.Application.Traffic;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Application.UnitTests.Traffic;

public sealed class GetDailyTrafficUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly Today = new(2026, 6, 9);

    private readonly FakeProductRepository _products = new();
    private readonly FakeTrafficSampleRepository _traffic = new();

    private GetDailyTrafficUseCase BuildUseCase() =>
        new(_products, _traffic, new FixedClock(Now));

    private Product Seed()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    private void AddView(ProductId productId, string path, DateTime at, string? referrer = null) =>
        _traffic.Items.Add(TrafficSample.Create(productId, path, referrer, null, at).Value!);

    [Fact]
    public async Task Should_Fail_When_ProductIsUnknown()
    {
        var result = await BuildUseCase().HandleAsync(
            new GetDailyTrafficQuery(Guid.NewGuid(), null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_DefaultToLast30Days_When_NoRangeSupplied()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetDailyTrafficQuery(product.Id.Value, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.From.Should().Be(Today.AddDays(-29));
        result.Value.To.Should().Be(Today);
        result.Value.Days.Should().HaveCount(30);
    }

    [Fact]
    public async Task Should_Fail_When_FromIsAfterTo()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new GetDailyTrafficQuery(product.Id.Value, Today, Today.AddDays(-1)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Analytics.InvalidRange:");
    }

    [Fact]
    public async Task Should_AggregateViews_When_SamplesAreInRange()
    {
        var product = Seed();
        AddView(product.Id, "/", Now.AddDays(-1), "https://github.com/x");
        AddView(product.Id, "/", Now, "https://github.com/x");
        AddView(product.Id, "/docs", Now);
        AddView(product.Id, "/old", Now.AddDays(-3));
        AddView(ProductId.New(), "/other", Now);

        var result = await BuildUseCase().HandleAsync(
            new GetDailyTrafficQuery(product.Id.Value, Today.AddDays(-1), Today), CancellationToken.None);

        var report = result.Value!;
        report.TotalPageViews.Should().Be(3);
        report.Days.Select(d => d.PageViews).Should().Equal(1, 2);
        report.TopPaths.Select(p => (p.Key, p.Count)).Should().Equal(("/", 2L), ("/docs", 1L));
        report.TopReferrers.Select(r => (r.Key, r.Count)).Should().Equal(("github", 2L), ("direct", 1L));
    }

    [Fact]
    public async Task Should_IncludeSamplesAtFromMidnight_When_RangeStarts()
    {
        var product = Seed();
        var from = Today.AddDays(-1);
        AddView(product.Id, "/edge", from.ToDateTime(TimeOnly.MinValue));

        var result = await BuildUseCase().HandleAsync(
            new GetDailyTrafficQuery(product.Id.Value, from, Today), CancellationToken.None);

        result.Value!.TotalPageViews.Should().Be(1);
    }
}
