using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class GetProductHealthUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();
    private readonly FakeDownloadSnapshotRepository _downloads = new();
    private readonly FakeRepositorySnapshotRepository _repositories = new();
    private readonly FakeAlertRepository _alerts = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public GetProductHealthUseCaseTests() => _products.Items.Add(_product);

    private GetProductHealthUseCase UseCase =>
        new(_products, _downloads, _repositories, _alerts, new FixedClock(Now));

    private void AddSnapshot(int daysAgo, long total, string? version = null) =>
        _downloads.Items.Add(DownloadSnapshot.Create(
            _product.Id, PackageRegistry.NuGet, PackageId.Create("outlet").Value!, total, Now.AddDays(-daysAgo), version).Value!);

    private void AddRepoSnapshot(int daysAgo, int openIssues, int stars) =>
        _repositories.Items.Add(RepositorySnapshot.Create(
            _product.Id, RepositoryName.Create("outlet/outlet").Value!, openIssues, stars, 1, Now.AddDays(-daysAgo)).Value!);

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await UseCase.HandleAsync(new GetProductHealthQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_ReturnNeutralComponents_When_NoDataIsStored()
    {
        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var health = result.Value!;
        health.Inputs.DaysSinceLatestRelease.Should().BeNull();
        health.Inputs.DownloadsPercentChange.Should().BeNull();
        health.Inputs.OpenIssuesGrowthPercent.Should().BeNull();
        health.Inputs.StarsGrowthPercent.Should().BeNull();
        health.Inputs.RecentCaptureFailures.Should().Be(0);
        health.SnapshotReliabilityScore.Should().Be(100);
        health.Label.Should().Be(HealthScore.WatchLabel);
    }

    [Fact]
    public async Task Should_ComputeDownloadTrend_When_TwoThirtyDayWindowsHaveData()
    {
        AddSnapshot(59, 0);
        AddSnapshot(40, 100);
        AddSnapshot(10, 300);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.DownloadsPercentChange.Should().Be(100m);
        result.Value.DownloadTrendScore.Should().Be(100);
    }

    [Fact]
    public async Task Should_SplitWindowsOnDayThirty_When_ComparingDownloadPeriods()
    {
        AddSnapshot(61, 0);
        AddSnapshot(60, 50);
        AddSnapshot(30, 150);
        AddSnapshot(29, 350);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.DownloadsPercentChange.Should().Be(100m);
    }

    [Fact]
    public async Task Should_DeriveReleaseAgeFromVersionChange_When_SnapshotsRecordedARelease()
    {
        AddSnapshot(20, 100, "1.0.0");
        AddSnapshot(5, 150, "1.1.0");

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.DaysSinceLatestRelease.Should().Be(5);
        result.Value.ReleaseFreshnessScore.Should().Be(100);
    }

    [Fact]
    public async Task Should_KeepFreshnessNeutral_When_VersionNeverChanged()
    {
        AddSnapshot(20, 100, "1.0.0");
        AddSnapshot(5, 150, "1.0.0");

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.DaysSinceLatestRelease.Should().BeNull();
        result.Value.ReleaseFreshnessScore.Should().Be(HealthScore.NeutralScore);
    }

    [Fact]
    public async Task Should_CompareRepoCountersOverThirtyDays_When_HistoryIsLongEnough()
    {
        AddRepoSnapshot(40, 10, 100);
        AddRepoSnapshot(1, 12, 110);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.OpenIssuesGrowthPercent.Should().Be(20m);
        result.Value.Inputs.StarsGrowthPercent.Should().Be(10m);
        result.Value.RepoActivityScore.Should().Be(50);
    }

    [Fact]
    public async Task Should_UseLastSnapshotBeforeTheWindow_When_OlderHistoryExists()
    {
        AddRepoSnapshot(50, 5, 100);
        AddRepoSnapshot(31, 5, 110);
        AddRepoSnapshot(15, 5, 130);
        AddRepoSnapshot(1, 5, 150);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.StarsGrowthPercent.Should().Be(36.4m);
    }

    [Fact]
    public async Task Should_FallBackToOldestSnapshot_When_HistoryIsShorterThanThirtyDays()
    {
        AddRepoSnapshot(10, 10, 100);
        AddRepoSnapshot(1, 10, 150);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.StarsGrowthPercent.Should().Be(50m);
        result.Value.Inputs.OpenIssuesGrowthPercent.Should().Be(0m);
    }

    [Fact]
    public async Task Should_KeepRepoActivityNeutral_When_OnlyOneSnapshotExists()
    {
        AddRepoSnapshot(1, 10, 100);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.StarsGrowthPercent.Should().BeNull();
        result.Value.RepoActivityScore.Should().Be(HealthScore.NeutralScore);
    }

    [Fact]
    public async Task Should_CountOnlyRecentSnapshotFailures_When_ScoringReliability()
    {
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.SnapshotFailure, "fail", Now.AddDays(-1)).Value!);
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.SnapshotFailure, "fail again", Now.AddDays(-2)).Value!);
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.SnapshotFailure, "window edge", Now.AddDays(-7)).Value!);
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.SnapshotFailure, "old fail", Now.AddDays(-10)).Value!);
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.DownloadsSpike, "spike", Now.AddDays(-1)).Value!);

        var result = await UseCase.HandleAsync(new GetProductHealthQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Inputs.RecentCaptureFailures.Should().Be(3);
        result.Value.SnapshotReliabilityScore.Should().Be(25);
    }
}
