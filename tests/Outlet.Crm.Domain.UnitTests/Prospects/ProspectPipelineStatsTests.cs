using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests.Prospects;

public sealed class ProspectPipelineStatsTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Prospect CreateProspect(ProspectStage stage = ProspectStage.New, decimal? value = null)
    {
        var prospect = Prospect.Create(
            ProductId.New(),
            null,
            "Ada",
            Email.Create("ada@example.com").Value!,
            null,
            value is { } amount ? Money.Create(amount, "EUR").Value! : null,
            Now).Value!;

        if (stage is ProspectStage.Lost)
        {
            prospect.Lose("no budget");
        }
        else if (stage is not ProspectStage.New)
        {
            prospect.Advance(stage);
        }

        return prospect;
    }

    [Fact]
    public void Should_ReturnEmptyReport_When_NoProspects()
    {
        var report = ProspectPipelineStats.Compute([]);

        report.TotalProspects.Should().Be(0);
        report.TotalEstimatedValue.Should().Be(0m);
        report.Stages.Should().HaveCount(5);
        report.Stages.Should().AllSatisfy(s =>
        {
            s.Count.Should().Be(0);
            s.TotalEstimatedValue.Should().Be(0m);
            s.ConversionRateToNext.Should().BeNull();
        });
    }

    [Fact]
    public void Should_CountProspectsPerStage_When_StagesAreMixed()
    {
        var report = ProspectPipelineStats.Compute(
        [
            CreateProspect(),
            CreateProspect(),
            CreateProspect(ProspectStage.Contacted),
            CreateProspect(ProspectStage.Qualified),
            CreateProspect(ProspectStage.Won),
            CreateProspect(ProspectStage.Lost),
        ]);

        report.TotalProspects.Should().Be(6);
        report.Stages.Select(s => (s.Stage, s.Count)).Should().Equal(
            (ProspectStage.New, 2),
            (ProspectStage.Contacted, 1),
            (ProspectStage.Qualified, 1),
            (ProspectStage.Won, 1),
            (ProspectStage.Lost, 1));
    }

    [Fact]
    public void Should_SumEstimatedValuesPerStage_When_SomeValuesAreMissing()
    {
        var report = ProspectPipelineStats.Compute(
        [
            CreateProspect(value: 100m),
            CreateProspect(),
            CreateProspect(ProspectStage.Qualified, 250.50m),
            CreateProspect(ProspectStage.Qualified, 49.50m),
        ]);

        report.Stages.Single(s => s.Stage == ProspectStage.New).TotalEstimatedValue.Should().Be(100m);
        report.Stages.Single(s => s.Stage == ProspectStage.Qualified).TotalEstimatedValue.Should().Be(300m);
        report.TotalEstimatedValue.Should().Be(400m);
    }

    [Fact]
    public void Should_ComputeConversionRates_When_ProspectsReachLaterStages()
    {
        // ever reached: New 4 (3 open/won + 1 lost), Contacted 3, Qualified 2, Won 1.
        var report = ProspectPipelineStats.Compute(
        [
            CreateProspect(ProspectStage.Contacted),
            CreateProspect(ProspectStage.Qualified),
            CreateProspect(ProspectStage.Won),
            CreateProspect(ProspectStage.Lost),
        ]);

        report.Stages.Single(s => s.Stage == ProspectStage.New).ConversionRateToNext.Should().Be(0.75m);
        report.Stages.Single(s => s.Stage == ProspectStage.Contacted).ConversionRateToNext.Should().Be(0.667m);
        report.Stages.Single(s => s.Stage == ProspectStage.Qualified).ConversionRateToNext.Should().Be(0.5m);
        report.Stages.Single(s => s.Stage == ProspectStage.Won).ConversionRateToNext.Should().BeNull();
        report.Stages.Single(s => s.Stage == ProspectStage.Lost).ConversionRateToNext.Should().BeNull();
    }

    [Fact]
    public void Should_ReturnNullConversionRate_When_StageWasNeverReached()
    {
        var report = ProspectPipelineStats.Compute([CreateProspect()]);

        report.Stages.Single(s => s.Stage == ProspectStage.New).ConversionRateToNext.Should().Be(0m);
        report.Stages.Single(s => s.Stage == ProspectStage.Contacted).ConversionRateToNext.Should().BeNull();
        report.Stages.Single(s => s.Stage == ProspectStage.Qualified).ConversionRateToNext.Should().BeNull();
    }

    [Fact]
    public void Should_CountLostProspectsOnlyTowardsNew_When_ComputingConversions()
    {
        var report = ProspectPipelineStats.Compute(
        [
            CreateProspect(ProspectStage.Lost),
            CreateProspect(ProspectStage.Lost),
        ]);

        report.Stages.Single(s => s.Stage == ProspectStage.New).ConversionRateToNext.Should().Be(0m);
        report.Stages.Single(s => s.Stage == ProspectStage.Lost).Count.Should().Be(2);
    }
}
