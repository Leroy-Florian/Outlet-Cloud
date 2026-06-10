using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests.Prospects;

public sealed class ProspectDetailsTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Prospect CreateProspect(Money? estimatedValue = null) =>
        Prospect.Create(
            ProductId.New(), null, "Ada", Email.Create("ada@example.com").Value!, "Acme", estimatedValue, Now).Value!;

    [Fact]
    public void Should_CarryEstimatedValue_When_ProvidedAtCreation()
    {
        var money = Money.Create(499.99m, "EUR").Value!;

        var prospect = CreateProspect(money);

        prospect.EstimatedValue.Should().Be(money);
        prospect.LossReason.Should().BeNull();
    }

    [Fact]
    public void Should_ReplaceEstimatedValueAndCompany_When_UpdatingDetails()
    {
        var prospect = CreateProspect();
        var money = Money.Create(1200m, "USD").Value!;

        var result = prospect.UpdateDetails(money, "  Initech  ");

        result.IsSuccess.Should().BeTrue();
        prospect.EstimatedValue.Should().Be(money);
        prospect.Company.Should().Be("Initech");
    }

    [Fact]
    public void Should_ClearFields_When_UpdatingDetailsWithNulls()
    {
        var prospect = CreateProspect(Money.Create(100m, "EUR").Value!);

        var result = prospect.UpdateDetails(null, "   ");

        result.IsSuccess.Should().BeTrue();
        prospect.EstimatedValue.Should().BeNull();
        prospect.Company.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_When_UpdatingAWonProspect()
    {
        var prospect = CreateProspect();
        prospect.Advance(ProspectStage.Won);

        var result = prospect.UpdateDetails(Money.Create(1m, "EUR").Value!, "X");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.AlreadyClosed);
        prospect.EstimatedValue.Should().BeNull();
        prospect.Company.Should().Be("Acme");
    }

    [Fact]
    public void Should_MarkLostWithReason_When_Losing()
    {
        var prospect = CreateProspect();
        prospect.Advance(ProspectStage.Qualified);

        var result = prospect.Lose("  chose a competitor  ");

        result.IsSuccess.Should().BeTrue();
        prospect.Stage.Should().Be(ProspectStage.Lost);
        prospect.LossReason.Should().Be("chose a competitor");
    }

    [Fact]
    public void Should_Fail_When_LosingWithoutReason()
    {
        var prospect = CreateProspect();

        var result = prospect.Lose("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.LossReasonRequired);
        prospect.Stage.Should().Be(ProspectStage.New);
        prospect.LossReason.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_When_LosingAClosedProspect()
    {
        var prospect = CreateProspect();
        prospect.Advance(ProspectStage.Won);

        var result = prospect.Lose("too late");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.AlreadyClosed);
        prospect.LossReason.Should().BeNull();
    }
}
