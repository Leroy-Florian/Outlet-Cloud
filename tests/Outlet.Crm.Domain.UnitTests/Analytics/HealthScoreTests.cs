using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class HealthScoreTests
{
    private static readonly ProductId SomeProduct = ProductId.New();

    private static DownloadSnapshot Snapshot(DateTime capturedAt, long total, string? version) =>
        DownloadSnapshot.Create(SomeProduct, PackageRegistry.NuGet, PackageId.Create("outlet").Value!, total, capturedAt, version).Value!;

    [Fact]
    public void Should_ScoreFull_When_ReleaseIsWithinFreshWindow()
    {
        HealthScore.ReleaseFreshnessScore(0).Should().Be(100);
        HealthScore.ReleaseFreshnessScore(HealthScore.FreshDays).Should().Be(100);
    }

    [Fact]
    public void Should_ScoreZero_When_ReleaseIsOlderThanStaleWindow()
    {
        HealthScore.ReleaseFreshnessScore(HealthScore.StaleDays).Should().Be(0);
        HealthScore.ReleaseFreshnessScore(700).Should().Be(0);
    }

    [Fact]
    public void Should_DecayLinearly_When_ReleaseAgeIsBetweenFreshAndStale()
    {
        var midpoint = (HealthScore.FreshDays + HealthScore.StaleDays) / 2;

        HealthScore.ReleaseFreshnessScore(midpoint).Should().BeInRange(49, 51);
        HealthScore.ReleaseFreshnessScore(60).Should().Be(91);
        HealthScore.ReleaseFreshnessScore(300).Should().Be(19);
    }

    [Fact]
    public void Should_StayNeutral_When_ReleaseDateIsUnknown()
    {
        HealthScore.ReleaseFreshnessScore(null).Should().Be(HealthScore.NeutralScore);
    }

    [Fact]
    public void Should_StayNeutral_When_DownloadTrendHasNoBaseline()
    {
        HealthScore.DownloadTrendScore(null).Should().Be(HealthScore.NeutralScore);
    }

    [Fact]
    public void Should_MapDownloadTrendLinearly_When_PercentChangeIsKnown()
    {
        HealthScore.DownloadTrendScore(0m).Should().Be(50);
        HealthScore.DownloadTrendScore(25m).Should().Be(75);
        HealthScore.DownloadTrendScore(-25m).Should().Be(25);
    }

    [Fact]
    public void Should_SaturateDownloadTrend_When_ChangeExceedsFiftyPercent()
    {
        HealthScore.DownloadTrendScore(80m).Should().Be(100);
        HealthScore.DownloadTrendScore(-90m).Should().Be(0);
    }

    [Fact]
    public void Should_StayNeutral_When_RepoHasNoComparableSnapshots()
    {
        HealthScore.RepoActivityScore(null, null).Should().Be(HealthScore.NeutralScore);
    }

    [Fact]
    public void Should_RewardStarsGrowth_When_RepoIsActive()
    {
        HealthScore.RepoActivityScore(0m, 10m).Should().Be(70);
    }

    [Fact]
    public void Should_PenalizeRisingIssues_When_RepoIssuesGrow()
    {
        HealthScore.RepoActivityScore(20m, 0m).Should().Be(30);
    }

    [Fact]
    public void Should_NotRewardFallingIssues_When_IssuesGoDown()
    {
        HealthScore.RepoActivityScore(-30m, 0m).Should().Be(HealthScore.NeutralScore);
    }

    [Fact]
    public void Should_UseAvailableSignal_When_OnlyOneRepoCounterIsKnown()
    {
        HealthScore.RepoActivityScore(null, 10m).Should().Be(70);
        HealthScore.RepoActivityScore(20m, null).Should().Be(30);
    }

    [Fact]
    public void Should_ClampRepoActivity_When_SignalsAreExtreme()
    {
        HealthScore.RepoActivityScore(0m, 1000m).Should().Be(100);
        HealthScore.RepoActivityScore(1000m, 0m).Should().Be(0);
    }

    [Fact]
    public void Should_ScoreFullReliability_When_NoRecentCaptureFailure()
    {
        HealthScore.SnapshotReliabilityScore(0).Should().Be(100);
    }

    [Fact]
    public void Should_PenalizeReliability_When_CapturesFailedRecently()
    {
        HealthScore.SnapshotReliabilityScore(1).Should().Be(75);
        HealthScore.SnapshotReliabilityScore(2).Should().Be(50);
        HealthScore.SnapshotReliabilityScore(5).Should().Be(0);
    }

    [Fact]
    public void Should_WeighComponents_When_ComputingTotal()
    {
        var health = HealthScore.Compute(new HealthInputs(10, 0m, 0m, 0m, 0));

        health.ReleaseFreshnessScore.Should().Be(100);
        health.DownloadTrendScore.Should().Be(50);
        health.RepoActivityScore.Should().Be(50);
        health.SnapshotReliabilityScore.Should().Be(100);
        health.Total.Should().Be((int)Math.Round((100 * 0.25m) + (50 * 0.35m) + (50 * 0.25m) + (100 * 0.15m)));
    }

    [Fact]
    public void Should_ScoreAllNeutralExceptReliability_When_NoDataIsAvailable()
    {
        var health = HealthScore.Compute(new HealthInputs(null, null, null, null, 0));

        health.ReleaseFreshnessScore.Should().Be(50);
        health.DownloadTrendScore.Should().Be(50);
        health.RepoActivityScore.Should().Be(50);
        health.SnapshotReliabilityScore.Should().Be(100);
        health.Total.Should().Be(58);
        health.Label.Should().Be(HealthScore.WatchLabel);
    }

    [Theory]
    [InlineData(80, HealthScore.ExcellentLabel)]
    [InlineData(95, HealthScore.ExcellentLabel)]
    [InlineData(60, HealthScore.GoodLabel)]
    [InlineData(79, HealthScore.GoodLabel)]
    [InlineData(40, HealthScore.WatchLabel)]
    [InlineData(59, HealthScore.WatchLabel)]
    [InlineData(39, HealthScore.StrugglingLabel)]
    [InlineData(0, HealthScore.StrugglingLabel)]
    public void Should_LabelInFrench_When_TotalFallsInEachBand(int total, string expected)
    {
        HealthScore.Label(total).Should().Be(expected);
    }

    [Fact]
    public void Should_FindLatestVersionChange_When_SnapshotsRecordedARelease()
    {
        var release = new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc);
        List<DownloadSnapshot> snapshots =
        [
            Snapshot(release.AddDays(-10), 100, "1.0.0"),
            Snapshot(release, 150, "1.1.0"),
            Snapshot(release.AddDays(2), 180, "1.1.0"),
        ];

        HealthScore.LatestObservedRelease(snapshots).Should().Be(release);
    }

    [Fact]
    public void Should_KeepMostRecentMarker_When_SeveralPackagesReleased()
    {
        var older = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc);
        var otherPackage = PackageId.Create("fluxpdf").Value!;
        List<DownloadSnapshot> snapshots =
        [
            Snapshot(older.AddDays(-5), 10, "1.0.0"),
            Snapshot(older, 20, "1.1.0"),
            DownloadSnapshot.Create(SomeProduct, PackageRegistry.Npm, otherPackage, 5, newer.AddDays(-5), "2.0.0").Value!,
            DownloadSnapshot.Create(SomeProduct, PackageRegistry.Npm, otherPackage, 9, newer, "2.1.0").Value!,
        ];

        HealthScore.LatestObservedRelease(snapshots).Should().Be(newer);
        HealthScore.LatestObservedRelease(snapshots.AsEnumerable().Reverse()).Should().Be(newer);
    }

    [Fact]
    public void Should_ReturnNull_When_NoVersionChangeWasEverObserved()
    {
        List<DownloadSnapshot> stable =
        [
            Snapshot(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), 100, "1.0.0"),
            Snapshot(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), 120, "1.0.0"),
        ];

        HealthScore.LatestObservedRelease(stable).Should().BeNull();
    }

    [Fact]
    public void Should_IgnoreUnknownVersions_When_LookingForReleaseMarkers()
    {
        List<DownloadSnapshot> snapshots =
        [
            Snapshot(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), 100, null),
            Snapshot(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), 120, "1.1.0"),
        ];

        HealthScore.LatestObservedRelease(snapshots).Should().BeNull();
    }

    [Fact]
    public void Should_ComputeGrowthPercent_When_BaselineIsPositive()
    {
        HealthScore.GrowthPercent(100, 150).Should().Be(50m);
        HealthScore.GrowthPercent(200, 100).Should().Be(-50m);
        HealthScore.GrowthPercent(3, 4).Should().Be(33.3m);
    }

    [Fact]
    public void Should_HandleZeroBaseline_When_ComputingGrowthPercent()
    {
        HealthScore.GrowthPercent(0, 10).Should().Be(100m);
        HealthScore.GrowthPercent(0, 0).Should().Be(0m);
    }
}
