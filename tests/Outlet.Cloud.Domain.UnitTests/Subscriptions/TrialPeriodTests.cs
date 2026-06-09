using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Domain.UnitTests.Subscriptions;

public sealed class TrialPeriodTests
{
    private static readonly DateOnly Day0 = new(2026, 6, 1);

    [Fact]
    public void Should_EndAfterDuration_When_Created()
    {
        var trial = TrialPeriod.Of(Day0, 14);

        trial.StartedOn.Should().Be(Day0);
        trial.EndsOn.Should().Be(Day0.AddDays(14));
    }

    [Fact]
    public void Should_Throw_When_DurationIsNotPositive()
    {
        var act = () => TrialPeriod.Of(Day0, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Throw_When_EndIsNotAfterStart()
    {
        var act = () => TrialPeriod.Between(Day0, Day0);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, 14)]
    [InlineData(5, 9)]
    [InlineData(13, 1)]
    [InlineData(14, 0)]
    [InlineData(20, 0)]
    public void Should_ComputeDaysRemaining(int daysElapsed, int expectedRemaining)
    {
        var trial = TrialPeriod.Of(Day0, 14);

        trial.DaysRemainingAsOf(Day0.AddDays(daysElapsed)).Should().Be(expectedRemaining);
    }

    [Fact]
    public void Should_ReportElapsed_When_TodayReachesEnd()
    {
        var trial = TrialPeriod.Of(Day0, 14);

        trial.HasElapsedAsOf(Day0.AddDays(13)).Should().BeFalse();
        trial.HasElapsedAsOf(Day0.AddDays(14)).Should().BeTrue();
    }
}
