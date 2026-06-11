using Outlet.Crm.Application.Feedback;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Feedback;

public sealed class GetNpsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeFeedbackRepository _feedback = new();
    private readonly FakeProductRepository _products = new();

    private GetNpsUseCase BuildUseCase() => new(_feedback, _products, new FixedClock(Now));

    private Product AddProduct()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    private void AddScored(ProductId productId, int? score, DateTime? receivedAt = null) =>
        _feedback.Items.Add(Domain.Feedback.Feedback.Create(
            productId, FeedbackCategory.Other, "feedback", null, null, score, receivedAt ?? Now).Value!);

    [Fact]
    public async Task Should_ComputeStandardNps_When_FeedbackIsScored()
    {
        var productId = ProductId.New();
        AddScored(productId, 10);
        AddScored(productId, 9);
        AddScored(productId, 8);
        AddScored(productId, 6);

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new NpsReport(25.0, 2, 1, 1, 4, 90));
    }

    [Fact]
    public async Task Should_ClassifyBoundaryScores_When_ComputingNps()
    {
        var productId = ProductId.New();
        AddScored(productId, 9);
        AddScored(productId, 7);
        AddScored(productId, 8);
        AddScored(productId, 6);
        AddScored(productId, 0);

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, null), CancellationToken.None);

        var report = result.Value!;
        report.Promoters.Should().Be(1);
        report.Passives.Should().Be(2);
        report.Detractors.Should().Be(2);
        report.Score.Should().Be(-20.0);
    }

    [Fact]
    public async Task Should_RoundNpsToOneDecimal_When_PercentagesAreNotWhole()
    {
        var productId = ProductId.New();
        AddScored(productId, 10);
        AddScored(productId, 10);
        AddScored(productId, 0);

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, null), CancellationToken.None);

        result.Value!.Score.Should().Be(33.3);
    }

    [Fact]
    public async Task Should_ReturnNullNps_When_NoFeedbackIsScored()
    {
        AddScored(ProductId.New(), null);

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new NpsReport(null, 0, 0, 0, 0, 90));
    }

    [Fact]
    public async Task Should_IgnoreUnscoredFeedback_When_ComputingNps()
    {
        var productId = ProductId.New();
        AddScored(productId, 10);
        AddScored(productId, null);

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, null), CancellationToken.None);

        result.Value.Should().Be(new NpsReport(100.0, 1, 0, 0, 1, 90));
    }

    [Fact]
    public async Task Should_ExcludeFeedbackOlderThanWindow_When_ComputingNps()
    {
        var productId = ProductId.New();
        AddScored(productId, 0, Now.AddDays(-91));
        AddScored(productId, 10, Now.AddDays(-90));

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, null), CancellationToken.None);

        result.Value.Should().Be(new NpsReport(100.0, 1, 0, 0, 1, 90));
    }

    [Fact]
    public async Task Should_UseRequestedWindow_When_DaysIsProvided()
    {
        var productId = ProductId.New();
        AddScored(productId, 0, Now.AddDays(-20));
        AddScored(productId, 10, Now.AddDays(-5));

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, 7), CancellationToken.None);

        result.Value.Should().Be(new NpsReport(100.0, 1, 0, 0, 1, 7));
    }

    [Fact]
    public async Task Should_FilterByProduct_When_ProductIdIsProvided()
    {
        var product = AddProduct();
        AddScored(product.Id, 10);
        AddScored(ProductId.New(), 0);

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(product.Id.Value, null), CancellationToken.None);

        result.Value.Should().Be(new NpsReport(100.0, 1, 0, 0, 1, 90));
    }

    [Fact]
    public async Task Should_Fail_When_ProductIsUnknown()
    {
        var productId = Guid.NewGuid();

        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(productId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound(new ProductId(productId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    public async Task Should_Fail_When_WindowIsNotPositive(int days)
    {
        var result = await BuildUseCase().HandleAsync(new GetNpsQuery(null, days), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Nps.InvalidWindow: 'days' must be a strictly positive number of days.");
    }
}
