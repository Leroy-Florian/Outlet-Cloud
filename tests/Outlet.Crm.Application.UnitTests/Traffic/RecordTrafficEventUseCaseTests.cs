using Outlet.Crm.Application.Traffic;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Traffic;

public sealed class RecordTrafficEventUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();
    private readonly FakeTrafficSampleRepository _traffic = new();

    private RecordTrafficEventUseCase BuildUseCase() =>
        new(_traffic, _products, new FixedClock(Now));

    private Product Seed()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    [Fact]
    public async Task Should_StoreSampleStampedByClock_When_NoOccurredAtSupplied()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new RecordTrafficEventCommand(product.Id.Value, "/docs", "https://google.com", "Mozilla/5.0", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var sample = _traffic.Items.Should().ContainSingle().Subject;
        sample.OccurredAt.Should().Be(Now);
        sample.Path.Should().Be("/docs");
        sample.ReferrerSource.Should().Be("google");
        sample.UserAgentCategory.Should().Be("browser");
        sample.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task Should_UseSuppliedOccurredAt_When_Provided()
    {
        var product = Seed();
        var occurredAt = Now.AddDays(-2);

        var result = await BuildUseCase().HandleAsync(
            new RecordTrafficEventCommand(product.Id.Value, "/docs", null, null, occurredAt),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _traffic.Items.Should().ContainSingle().Which.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public async Task Should_Fail_When_ProductIsUnknown()
    {
        var result = await BuildUseCase().HandleAsync(
            new RecordTrafficEventCommand(Guid.NewGuid(), "/docs", null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
        _traffic.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_PathIsBlank()
    {
        var product = Seed();

        var result = await BuildUseCase().HandleAsync(
            new RecordTrafficEventCommand(product.Id.Value, "  ", null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("TrafficSample.PathRequired:");
        _traffic.Items.Should().BeEmpty();
    }
}
