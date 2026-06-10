using Outlet.Crm.Application.ApiMetrics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.ApiMetrics;

public sealed class RecordApiMetricUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeApiMetricRepository _repository = new();
    private readonly FakeProductRepository _products = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public RecordApiMetricUseCaseTests() => _products.Items.Add(_product);

    private RecordApiMetricUseCase UseCase => new(_repository, _products, new FixedClock(Now));

    [Fact]
    public async Task Should_StoreSample_When_CommandIsValid()
    {
        var result = await UseCase.HandleAsync(
            new RecordApiMetricCommand(_product.Id.Value, "/api/items", 200, 12.5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var sample = _repository.Items.Should().ContainSingle().Subject;
        sample.OccurredAt.Should().Be(Now);
        sample.ProductId.Should().Be(_product.Id);
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await UseCase.HandleAsync(
            new RecordApiMetricCommand(Guid.NewGuid(), "/api/items", 200, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_Fail_When_DurationIsNegative()
    {
        var result = await UseCase.HandleAsync(
            new RecordApiMetricCommand(_product.Id.Value, "/api/items", 200, -1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ComputeStatisticsForProductWindow_When_Queried()
    {
        await UseCase.HandleAsync(new RecordApiMetricCommand(_product.Id.Value, "/api/items", 200, 10), CancellationToken.None);
        await UseCase.HandleAsync(new RecordApiMetricCommand(_product.Id.Value, "/api/items", 500, 30), CancellationToken.None);
        var query = new GetEndpointStatisticsUseCase(_repository);

        var result = await query.HandleAsync(
            new GetEndpointStatisticsQuery(_product.Id.Value, Now.AddHours(-1)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stats = result.Value!.Should().ContainSingle().Subject;
        stats.RequestCount.Should().Be(2);
        stats.ErrorCount.Should().Be(1);
    }
}
