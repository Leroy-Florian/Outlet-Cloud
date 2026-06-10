using Outlet.Crm.Application.Prospects;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Application.UnitTests.Prospects;

public sealed class AdvanceProspectStageUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProspectRepository _repository = new();

    [Fact]
    public async Task Should_AdvanceStage_When_ProspectExists()
    {
        var prospect = Prospect.Create(ProductId.New(), null, "Ada", Email.Create("ada@example.com").Value!, null, null, Now).Value!;
        _repository.Items.Add(prospect);
        var useCase = new AdvanceProspectStageUseCase(_repository);

        var result = await useCase.HandleAsync(
            new AdvanceProspectStageCommand(prospect.Id, ProspectStage.Contacted), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        prospect.Stage.Should().Be(ProspectStage.Contacted);
        _repository.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_TransitionIsInvalid()
    {
        var prospect = Prospect.Create(ProductId.New(), null, "Ada", Email.Create("ada@example.com").Value!, null, null, Now).Value!;
        prospect.Advance(ProspectStage.Qualified);
        _repository.Items.Add(prospect);
        var useCase = new AdvanceProspectStageUseCase(_repository);

        var result = await useCase.HandleAsync(
            new AdvanceProspectStageCommand(prospect.Id, ProspectStage.Contacted), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Prospect.InvalidTransition:");
        _repository.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_ProspectIsMissing()
    {
        var useCase = new AdvanceProspectStageUseCase(_repository);

        var result = await useCase.HandleAsync(
            new AdvanceProspectStageCommand(ProspectId.New(), ProspectStage.Contacted), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Prospect.NotFound:");
        _repository.UpdateCount.Should().Be(0);
    }
}
