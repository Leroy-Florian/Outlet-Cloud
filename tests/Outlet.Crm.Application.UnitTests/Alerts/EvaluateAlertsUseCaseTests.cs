using Outlet.Crm.Application.Alerts;
using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Alerts;

public sealed class EvaluateAlertsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();
    private readonly FakeDownloadSnapshotRepository _downloadSnapshots = new();
    private readonly FakeRepositorySnapshotRepository _repositorySnapshots = new();
    private readonly FakeAlertRepository _alerts = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public EvaluateAlertsUseCaseTests() => _products.Items.Add(_product);

    private EvaluateAlertsUseCase UseCase =>
        new(_products, _downloadSnapshots, _repositorySnapshots, _alerts, new FixedClock(Now));

    private void AddCumulativeSnapshots(params long[] dailyNewDownloads)
    {
        // One snapshot per day ending today; the first one is the day-before baseline.
        var firstDay = Now.AddDays(-dailyNewDownloads.Length);
        long total = 0;
        AddSnapshot(total, firstDay);
        for (var i = 0; i < dailyNewDownloads.Length; i++)
        {
            total += dailyNewDownloads[i];
            AddSnapshot(total, firstDay.AddDays(i + 1));
        }
    }

    private void AddSnapshot(long total, DateTime capturedAt) =>
        _downloadSnapshots.Items.Add(DownloadSnapshot.Create(
            _product.Id, PackageRegistry.Npm, PackageId.Create("outlet").Value!, total, capturedAt).Value!);

    private void AddStarSnapshots(int previousStars, int latestStars)
    {
        var repository = RepositoryName.Create("acme/outlet").Value!;
        _repositorySnapshots.Items.Add(RepositorySnapshot.Create(_product.Id, repository, 0, previousStars, 0, Now.AddDays(-1)).Value!);
        _repositorySnapshots.Items.Add(RepositorySnapshot.Create(_product.Id, repository, 0, latestStars, 0, Now).Value!);
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
        _alerts.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RaiseDownloadsSpike_When_TodayDoublesTheWeeklyAverage()
    {
        AddCumulativeSnapshots(10, 10, 10, 10, 10, 10, 10, 1000);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var alert = result.Value!.Should().ContainSingle().Subject;
        alert.Type.Should().Be(AlertType.DownloadsSpike);
        alert.ProductId.Should().Be(_product.Id);
        alert.TriggeredAt.Should().Be(Now);
        _alerts.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_RaiseDownloadsDrop_When_TodayCollapsesAgainstTheWeeklyAverage()
    {
        AddCumulativeSnapshots(100, 100, 100, 100, 100, 100, 100, 10);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        result.Value!.Should().ContainSingle().Which.Type.Should().Be(AlertType.DownloadsDrop);
    }

    [Fact]
    public async Task Should_RaiseNothing_When_DownloadsAreSteady()
    {
        AddCumulativeSnapshots(100, 100, 100, 100, 100, 100, 100, 100);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
        _alerts.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RaiseStarsMilestone_When_StarsCrossAHundred()
    {
        AddStarSnapshots(90, 120);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        var alert = result.Value!.Should().ContainSingle().Subject;
        alert.Type.Should().Be(AlertType.StarsMilestone);
        alert.Message.Should().Contain("acme/outlet");
    }

    [Fact]
    public async Task Should_RaiseNothing_When_OnlyOneRepositorySnapshotExists()
    {
        _repositorySnapshots.Items.Add(RepositorySnapshot.Create(
            _product.Id, RepositoryName.Create("acme/outlet").Value!, 0, 500, 0, Now).Value!);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RaiseSnapshotFailure_When_CaptureReportsContainFailures()
    {
        List<SnapshotCaptureReport> reports =
        [
            new("npm:outlet", true, null),
            new("nuget:outlet", false, "NuGetStats.HttpError: 503"),
        ];

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value, reports), CancellationToken.None);

        var alert = result.Value!.Should().ContainSingle().Subject;
        alert.Type.Should().Be(AlertType.SnapshotFailure);
        alert.Message.Should().Contain("nuget:outlet").And.NotContain("npm:outlet");
    }

    [Fact]
    public async Task Should_RaiseNoSnapshotFailure_When_AllCaptureReportsSucceeded()
    {
        List<SnapshotCaptureReport> reports = [new("npm:outlet", true, null)];

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value, reports), CancellationToken.None);

        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotDuplicateAlert_When_SameTypeAlreadyTriggeredToday()
    {
        AddCumulativeSnapshots(10, 10, 10, 10, 10, 10, 10, 1000);
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.DownloadsSpike, "earlier today", Now.AddHours(-3)).Value!);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
        _alerts.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_RaiseAlertAgain_When_SameTypeTriggeredOnAPreviousDay()
    {
        AddCumulativeSnapshots(10, 10, 10, 10, 10, 10, 10, 1000);
        _alerts.Items.Add(Alert.Create(_product.Id, AlertType.DownloadsSpike, "yesterday", Now.AddDays(-1)).Value!);

        var result = await UseCase.HandleAsync(new EvaluateAlertsCommand(_product.Id.Value), CancellationToken.None);

        result.Value!.Should().ContainSingle();
        _alerts.Items.Should().HaveCount(2);
    }
}
