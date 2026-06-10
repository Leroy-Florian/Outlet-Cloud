using Outlet.Crm.Application.Prospects;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Application.UnitTests.Prospects;

public sealed class ProspectPipelineUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProspectRepository _prospects = new();
    private readonly FakeProductRepository _products = new();
    private readonly FakeOrganizationRepository _organizations = new();
    private readonly FixedClock _clock = new(Now);

    private Prospect AddProspect(Money? estimatedValue = null)
    {
        var prospect = Prospect.Create(
            ProductId.New(), null, "Ada", Email.Create("ada@example.com").Value!, null, estimatedValue, Now).Value!;
        _prospects.Items.Add(prospect);
        return prospect;
    }

    [Fact]
    public async Task Should_StoreEstimatedValue_When_CreatingWithAmountAndCurrency()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new CreateProspectUseCase(_prospects, _products, _organizations, _clock);

        var result = await useCase.HandleAsync(new CreateProspectCommand(
            product.Id.Value, null, "Ada", "ada@example.com", "Acme", 499.99m, "usd"));

        result.IsSuccess.Should().BeTrue();
        var stored = _prospects.Items.Single();
        stored.EstimatedValue!.Amount.Should().Be(499.99m);
        stored.EstimatedValue.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Should_DefaultCurrencyToEur_When_CreatingWithAmountOnly()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new CreateProspectUseCase(_prospects, _products, _organizations, _clock);

        var result = await useCase.HandleAsync(new CreateProspectCommand(
            product.Id.Value, null, "Ada", "ada@example.com", null, 100m));

        result.IsSuccess.Should().BeTrue();
        _prospects.Items.Single().EstimatedValue!.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task Should_Fail_When_CreatingWithNegativeEstimatedValue()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new CreateProspectUseCase(_prospects, _products, _organizations, _clock);

        var result = await useCase.HandleAsync(new CreateProspectCommand(
            product.Id.Value, null, "Ada", "ada@example.com", null, -1m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Money.Negative:");
        _prospects.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_UpdateEstimatedValueAndCompany_When_ProspectIsOpen()
    {
        var prospect = AddProspect();
        var useCase = new UpdateProspectUseCase(_prospects);

        var result = await useCase.HandleAsync(new UpdateProspectCommand(prospect.Id, 1200m, "USD", "Initech"));

        result.IsSuccess.Should().BeTrue();
        prospect.EstimatedValue!.Amount.Should().Be(1200m);
        prospect.EstimatedValue.Currency.Should().Be("USD");
        prospect.Company.Should().Be("Initech");
        _prospects.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ClearEstimatedValue_When_UpdatingWithNullAmount()
    {
        var prospect = AddProspect(Money.Create(100m, "EUR").Value!);
        var useCase = new UpdateProspectUseCase(_prospects);

        var result = await useCase.HandleAsync(new UpdateProspectCommand(prospect.Id, null, null, null));

        result.IsSuccess.Should().BeTrue();
        prospect.EstimatedValue.Should().BeNull();
    }

    [Fact]
    public async Task Should_Fail_When_UpdatingUnknownProspect()
    {
        var useCase = new UpdateProspectUseCase(_prospects);
        var id = ProspectId.New();

        var result = await useCase.HandleAsync(new UpdateProspectCommand(id, null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.NotFound(id));
    }

    [Fact]
    public async Task Should_NotPersist_When_UpdatingWithInvalidCurrency()
    {
        var prospect = AddProspect();
        var useCase = new UpdateProspectUseCase(_prospects);

        var result = await useCase.HandleAsync(new UpdateProspectCommand(prospect.Id, 10m, "EURO", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Money.InvalidCurrency:");
        _prospects.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_NotPersist_When_UpdatingAClosedProspect()
    {
        var prospect = AddProspect();
        prospect.Advance(ProspectStage.Won);
        var useCase = new UpdateProspectUseCase(_prospects);

        var result = await useCase.HandleAsync(new UpdateProspectCommand(prospect.Id, 10m, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.AlreadyClosed);
        _prospects.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_MarkProspectLost_When_ReasonIsProvided()
    {
        var prospect = AddProspect();
        var useCase = new LoseProspectUseCase(_prospects);

        var result = await useCase.HandleAsync(new LoseProspectCommand(prospect.Id, "no budget"));

        result.IsSuccess.Should().BeTrue();
        prospect.Stage.Should().Be(ProspectStage.Lost);
        prospect.LossReason.Should().Be("no budget");
        _prospects.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_LosingUnknownProspect()
    {
        var useCase = new LoseProspectUseCase(_prospects);
        var id = ProspectId.New();

        var result = await useCase.HandleAsync(new LoseProspectCommand(id, "no budget"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.NotFound(id));
    }

    [Fact]
    public async Task Should_NotPersist_When_LosingWithoutReason()
    {
        var prospect = AddProspect();
        var useCase = new LoseProspectUseCase(_prospects);

        var result = await useCase.HandleAsync(new LoseProspectCommand(prospect.Id, " "));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.LossReasonRequired);
        _prospects.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_AggregatePipeline_When_QueryingStats()
    {
        AddProspect(Money.Create(100m, "EUR").Value!);
        var qualified = AddProspect(Money.Create(300m, "EUR").Value!);
        qualified.Advance(ProspectStage.Qualified);
        var useCase = new GetProspectPipelineStatsUseCase(_prospects);

        var result = await useCase.HandleAsync(new GetProspectPipelineStatsQuery());

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalProspects.Should().Be(2);
        report.TotalEstimatedValue.Should().Be(400m);
        report.Stages.Single(s => s.Stage == ProspectStage.Qualified).Count.Should().Be(1);
        report.Stages.Single(s => s.Stage == ProspectStage.New).ConversionRateToNext.Should().Be(0.5m);
    }
}
