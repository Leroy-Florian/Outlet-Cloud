using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class DailyDownloadsTests
{
    private static readonly DateTime Day1Noon = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly Day1 = new(2026, 6, 1);

    private static readonly ProductId Product = ProductId.New();

    private static DownloadSnapshot Snapshot(string package, long total, DateTime at, PackageRegistry registry = PackageRegistry.NuGet) =>
        DownloadSnapshot.Create(Product, registry, PackageId.Create(package).Value!, total, at).Value!;

    [Fact]
    public void Should_ReturnZeroFilledDays_When_NoSnapshots()
    {
        var report = DailyDownloads.Compute([], Day1, Day1.AddDays(2));

        report.From.Should().Be(Day1);
        report.To.Should().Be(Day1.AddDays(2));
        report.TotalDownloads.Should().Be(0);
        report.Sources.Should().BeEmpty();
        report.Days.Select(d => d.Date).Should().Equal(Day1, Day1.AddDays(1), Day1.AddDays(2));
        report.Days.Select(d => d.Downloads).Should().Equal(0, 0, 0);
    }

    [Fact]
    public void Should_ReturnNoDownloads_When_SingleSnapshotHasNoBaseline()
    {
        var report = DailyDownloads.Compute([Snapshot("outlet.cli", 100, Day1Noon)], Day1, Day1.AddDays(1));

        report.TotalDownloads.Should().Be(0);
        report.Sources.Should().ContainSingle().Which.Downloads.Should().Be(0);
    }

    [Fact]
    public void Should_AttributeDeltaToCaptureDay_When_ConsecutiveSnapshotsExist()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("outlet.cli", 100, Day1Noon),
            Snapshot("outlet.cli", 130, Day1Noon.AddDays(1)),
            Snapshot("outlet.cli", 175, Day1Noon.AddDays(2)),
        ], Day1, Day1.AddDays(2));

        report.Days.Select(d => d.Downloads).Should().Equal(0, 30, 45);
        report.TotalDownloads.Should().Be(75);
        var source = report.Sources.Should().ContainSingle().Subject;
        source.Downloads.Should().Be(75);
        source.Days.Select(d => d.Downloads).Should().Equal(0, 30, 45);
    }

    [Fact]
    public void Should_ClampDeltaToZero_When_TotalsDecrease()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("outlet.cli", 100, Day1Noon),
            Snapshot("outlet.cli", 80, Day1Noon.AddDays(1)),
            Snapshot("outlet.cli", 90, Day1Noon.AddDays(2)),
        ], Day1, Day1.AddDays(2));

        report.Days.Select(d => d.Downloads).Should().Equal(0, 0, 10);
        report.TotalDownloads.Should().Be(10);
    }

    [Fact]
    public void Should_UseBaselineBeforeRange_When_FirstSnapshotPrecedesFrom()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("outlet.cli", 100, Day1Noon.AddDays(-1)),
            Snapshot("outlet.cli", 120, Day1Noon),
        ], Day1, Day1);

        report.TotalDownloads.Should().Be(20);
        report.Days.Should().ContainSingle().Which.Downloads.Should().Be(20);
    }

    [Fact]
    public void Should_ExcludeDeltas_When_CapturedOutsideTheRange()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("outlet.cli", 100, Day1Noon.AddDays(-2)),
            Snapshot("outlet.cli", 110, Day1Noon.AddDays(-1)),
            Snapshot("outlet.cli", 130, Day1Noon),
            Snapshot("outlet.cli", 170, Day1Noon.AddDays(1)),
            Snapshot("outlet.cli", 250, Day1Noon.AddDays(2)),
        ], Day1, Day1.AddDays(1));

        report.Days.Select(d => d.Downloads).Should().Equal(20, 40);
        report.TotalDownloads.Should().Be(60);
    }

    [Fact]
    public void Should_AccumulateSameDayDeltas_When_SeveralSnapshotsLandOnOneDay()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("outlet.cli", 100, Day1Noon.AddHours(-13)),
            Snapshot("outlet.cli", 110, Day1Noon.AddHours(-2)),
            Snapshot("outlet.cli", 125, Day1Noon),
        ], Day1, Day1);

        report.Days.Should().ContainSingle().Which.Downloads.Should().Be(25);
    }

    [Fact]
    public void Should_BreakDownPerSource_When_SeveralPackagesAreTracked()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("zeta", 10, Day1Noon, PackageRegistry.NuGet),
            Snapshot("zeta", 16, Day1Noon.AddDays(1), PackageRegistry.NuGet),
            Snapshot("alpha", 5, Day1Noon, PackageRegistry.Npm),
            Snapshot("alpha", 9, Day1Noon.AddDays(1), PackageRegistry.Npm),
            Snapshot("beta", 1, Day1Noon, PackageRegistry.Npm),
            Snapshot("beta", 2, Day1Noon.AddDays(1), PackageRegistry.Npm),
        ], Day1, Day1.AddDays(1));

        report.Sources.Select(s => (s.Registry, s.PackageId)).Should().Equal(
            (PackageRegistry.NuGet, "zeta"),
            (PackageRegistry.Npm, "alpha"),
            (PackageRegistry.Npm, "beta"));
        report.Sources.Select(s => s.Downloads).Should().Equal(6, 4, 1);
        report.Days.Select(d => d.Downloads).Should().Equal(0, 11);
        report.TotalDownloads.Should().Be(11);
    }

    [Fact]
    public void Should_OrderSnapshots_When_ProvidedOutOfOrder()
    {
        var report = DailyDownloads.Compute(
        [
            Snapshot("outlet.cli", 130, Day1Noon.AddDays(1)),
            Snapshot("outlet.cli", 100, Day1Noon),
        ], Day1, Day1.AddDays(1));

        report.Days.Select(d => d.Downloads).Should().Equal(0, 30);
    }
}
