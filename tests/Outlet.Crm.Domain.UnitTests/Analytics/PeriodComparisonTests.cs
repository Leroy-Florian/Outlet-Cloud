using Outlet.Crm.Domain.Analytics;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class PeriodComparisonTests
{
    [Fact]
    public void Should_ReturnNullPercentChange_When_PreviousPeriodIsZero()
    {
        var comparison = PeriodComparison.Of(42, 0);

        comparison.CurrentPeriod.Should().Be(42);
        comparison.PreviousPeriod.Should().Be(0);
        comparison.PercentChange.Should().BeNull();
        comparison.Direction.Should().Be(TrendDirection.Up);
    }

    [Fact]
    public void Should_ReturnFlat_When_BothPeriodsAreZero()
    {
        var comparison = PeriodComparison.Of(0, 0);

        comparison.PercentChange.Should().BeNull();
        comparison.Direction.Should().Be(TrendDirection.Flat);
    }

    [Fact]
    public void Should_ComputePositivePercentChange_When_CurrentIsHigher()
    {
        var comparison = PeriodComparison.Of(150, 100);

        comparison.PercentChange.Should().Be(50.0m);
        comparison.Direction.Should().Be(TrendDirection.Up);
    }

    [Fact]
    public void Should_ComputeNegativePercentChange_When_CurrentIsLower()
    {
        var comparison = PeriodComparison.Of(75, 100);

        comparison.PercentChange.Should().Be(-25.0m);
        comparison.Direction.Should().Be(TrendDirection.Down);
    }

    [Fact]
    public void Should_ReturnMinusHundred_When_CurrentDropsToZero()
    {
        var comparison = PeriodComparison.Of(0, 40);

        comparison.PercentChange.Should().Be(-100.0m);
        comparison.Direction.Should().Be(TrendDirection.Down);
    }

    [Fact]
    public void Should_RoundToOneDecimal_When_ChangeIsFractional()
    {
        PeriodComparison.Of(1, 3).PercentChange.Should().Be(-66.7m);
        PeriodComparison.Of(2, 3).PercentChange.Should().Be(-33.3m);
    }

    [Fact]
    public void Should_ReturnZeroFlat_When_PeriodsAreEqual()
    {
        var comparison = PeriodComparison.Of(10, 10);

        comparison.PercentChange.Should().Be(0m);
        comparison.Direction.Should().Be(TrendDirection.Flat);
    }
}
