using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class DownloadTrendTests
{
    private static readonly DateTime Day1 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly ProductId Product = ProductId.New();

    private static DownloadSnapshot Snapshot(long total, DateTime at) =>
        DownloadSnapshot.Create(Product, PackageRegistry.NuGet, PackageId.Create("outlet.cli").Value!, total, at).Value!;

    [Fact]
    public void Should_ReturnEmpty_When_NoSnapshots()
    {
        var points = DownloadTrend.FromSnapshots([]);

        points.Should().BeEmpty();
    }

    [Fact]
    public void Should_ComputeDeltas_When_SnapshotsAreOutOfOrder()
    {
        var points = DownloadTrend.FromSnapshots(
        [
            Snapshot(150, Day1.AddDays(2)),
            Snapshot(100, Day1),
            Snapshot(120, Day1.AddDays(1)),
        ]);

        points.Select(p => p.Delta).Should().Equal(0, 20, 30);
        points.Select(p => p.TotalDownloads).Should().Equal(100, 120, 150);
    }

    [Fact]
    public void Should_RejectNegativeCount_When_CreatingSnapshot()
    {
        var result = DownloadSnapshot.Create(Product, PackageRegistry.NuGet, PackageId.Create("outlet.cli").Value!, -1, Day1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("DownloadSnapshot.NegativeCount:");
    }

    [Fact]
    public void Should_NormalizePackageId_When_Created()
    {
        var result = PackageId.Create("  Outlet.CLI ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("outlet.cli");
    }

    [Fact]
    public void Should_RejectEmptyPackageId_When_Created()
    {
        var result = PackageId.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("PackageId.Empty:");
    }
}
