using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests.Prospects;

public sealed class ProspectTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Prospect CreateProspect() =>
        Prospect.Create(ProductId.New(), null, "Ada Lovelace", Email.Create("ada@example.com").Value!, "Analytical Engines", Now).Value!;

    [Fact]
    public void Should_StartInNewStage_When_Created()
    {
        CreateProspect().Stage.Should().Be(ProspectStage.New);
    }

    [Fact]
    public void Should_Fail_When_NameIsBlank()
    {
        var result = Prospect.Create(ProductId.New(), null, "  ", Email.Create("ada@example.com").Value!, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.NameRequired);
    }

    [Fact]
    public void Should_AdvanceStage_When_TargetIsForward()
    {
        var prospect = CreateProspect();

        var result = prospect.Advance(ProspectStage.Contacted);

        result.IsSuccess.Should().BeTrue();
        prospect.Stage.Should().Be(ProspectStage.Contacted);
    }

    [Fact]
    public void Should_Fail_When_AdvancingBackward()
    {
        var prospect = CreateProspect();
        prospect.Advance(ProspectStage.Qualified);

        var result = prospect.Advance(ProspectStage.Contacted);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.InvalidTransition);
    }

    [Fact]
    public void Should_AllowMarkingLost_When_FromAnyOpenStage()
    {
        var prospect = CreateProspect();

        var result = prospect.Advance(ProspectStage.Lost);

        result.IsSuccess.Should().BeTrue();
        prospect.Stage.Should().Be(ProspectStage.Lost);
    }

    [Fact]
    public void Should_Fail_When_AdvancingAClosedProspect()
    {
        var prospect = CreateProspect();
        prospect.Advance(ProspectStage.Won);

        var result = prospect.Advance(ProspectStage.Lost);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.AlreadyClosed);
    }

    [Fact]
    public void Should_AppendInteraction_When_Recorded()
    {
        var prospect = CreateProspect();

        prospect.RecordInteraction("email", "Intro call scheduled", Now);

        prospect.Interactions.Should().ContainSingle().Which.Channel.Should().Be("email");
    }
}
