using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Analytics;

public sealed class DownloadSnapshotTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_HaveNoLatestVersion_When_NoneIsProvided()
    {
        var snapshot = DownloadSnapshot.Create(
            ProductId.New(), PackageRegistry.NuGet, PackageId.Create("outlet").Value!, 100, Now).Value!;

        snapshot.LatestVersion.Should().BeNull();
        snapshot.TotalDownloads.Should().Be(100);
        snapshot.CapturedAt.Should().Be(Now);
    }

    [Fact]
    public void Should_KeepLatestVersion_When_Provided()
    {
        var snapshot = DownloadSnapshot.Create(
            ProductId.New(), PackageRegistry.Npm, PackageId.Create("outlet").Value!, 100, Now, "2.4.1").Value!;

        snapshot.LatestVersion.Should().Be("2.4.1");
    }

    [Fact]
    public void Should_Fail_When_DownloadCountIsNegative()
    {
        var result = DownloadSnapshot.Create(
            ProductId.New(), PackageRegistry.NuGet, PackageId.Create("outlet").Value!, -1, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("DownloadSnapshot.NegativeCount:");
    }
}
