using Outlet.Crm.Domain.Alerts;

namespace Outlet.Crm.Domain.UnitTests.Alerts;

public sealed class AlertRulesTests
{
    [Fact]
    public void Should_SignalSpike_When_TodayExceedsTwiceTheAverageAndTheFloor()
    {
        var signal = AlertRules.EvaluateDownloads("npm:outlet", 101, 50m);

        signal.Should().NotBeNull();
        signal!.Type.Should().Be(AlertType.DownloadsSpike);
        signal.Message.Should().Contain("npm:outlet").And.Contain("101");
    }

    [Fact]
    public void Should_NotSignalSpike_When_TodayIsExactlyTwiceTheAverage()
    {
        AlertRules.EvaluateDownloads("npm:outlet", 100, 50m).Should().BeNull();
    }

    [Fact]
    public void Should_NotSignalSpike_When_TodayIsAtTheAbsoluteFloor()
    {
        AlertRules.EvaluateDownloads("npm:outlet", 50, 10m).Should().BeNull();
    }

    [Fact]
    public void Should_SignalSpike_When_TodayIsJustAboveTheAbsoluteFloor()
    {
        var signal = AlertRules.EvaluateDownloads("npm:outlet", 51, 10m);

        signal!.Type.Should().Be(AlertType.DownloadsSpike);
    }

    [Fact]
    public void Should_SignalDrop_When_TodayFallsBelowHalfTheAverage()
    {
        var signal = AlertRules.EvaluateDownloads("nuget:outlet", 29, 60m);

        signal.Should().NotBeNull();
        signal!.Type.Should().Be(AlertType.DownloadsDrop);
        signal.Message.Should().Contain("nuget:outlet").And.Contain("29");
    }

    [Fact]
    public void Should_NotSignalDrop_When_TodayIsExactlyHalfTheAverage()
    {
        AlertRules.EvaluateDownloads("nuget:outlet", 30, 60m).Should().BeNull();
    }

    [Fact]
    public void Should_NotSignalDrop_When_BaselineIsAtTheFloor()
    {
        AlertRules.EvaluateDownloads("nuget:outlet", 0, 50m).Should().BeNull();
    }

    [Fact]
    public void Should_SignalDrop_When_BaselineIsJustAboveTheFloor()
    {
        var signal = AlertRules.EvaluateDownloads("nuget:outlet", 0, 51m);

        signal!.Type.Should().Be(AlertType.DownloadsDrop);
    }

    [Fact]
    public void Should_ReturnNull_When_DownloadsAreWithinNormalRange()
    {
        AlertRules.EvaluateDownloads("npm:outlet", 60, 55m).Should().BeNull();
    }

    [Theory]
    [InlineData(90, 110, 100)]
    [InlineData(999, 1000, 1000)]
    [InlineData(150, 210, 200)]
    [InlineData(8, 12, 10)]
    [InlineData(0, 10, 10)]
    [InlineData(199, 205, 200)]
    [InlineData(950, 1500, 1500 / 100 * 100)]
    public void Should_DetectMilestone_When_StarsCrossIt(int previous, int latest, long expected)
    {
        AlertRules.CrossedMilestone(previous, latest).Should().Be(expected);
    }

    [Theory]
    [InlineData(95, 99)]
    [InlineData(12, 95)]
    [InlineData(0, 9)]
    [InlineData(100, 100)]
    [InlineData(120, 50)]
    [InlineData(100, 199)]
    public void Should_ReturnNull_When_NoMilestoneIsCrossed(int previous, int latest)
    {
        AlertRules.CrossedMilestone(previous, latest).Should().BeNull();
    }

    [Fact]
    public void Should_ReportLargestMilestone_When_SeveralAreCrossedAtOnce()
    {
        AlertRules.CrossedMilestone(5, 1234).Should().Be(1200);
    }

    [Fact]
    public void Should_SignalStarsMilestone_When_MilestoneIsCrossed()
    {
        var signal = AlertRules.EvaluateStars("acme/outlet", 90, 120);

        signal.Should().NotBeNull();
        signal!.Type.Should().Be(AlertType.StarsMilestone);
        signal.Message.Should().Contain("acme/outlet").And.Contain("100").And.Contain("120");
    }

    [Fact]
    public void Should_ReturnNull_When_StarsDidNotCrossAMilestone()
    {
        AlertRules.EvaluateStars("acme/outlet", 110, 120).Should().BeNull();
    }
}
