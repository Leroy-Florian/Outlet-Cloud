using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Application.Products;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.UnitTests.Products;

public sealed class ProductUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();

    [Fact]
    public async Task Should_PersistProduct_When_CommandIsValid()
    {
        var useCase = new CreateProductUseCase(_products, new FixedClock(Now));

        var result = await useCase.HandleAsync(new CreateProductCommand("Accordent", "7 packages npm"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _products.Items.Should().ContainSingle().Which.Name.Should().Be("Accordent");
    }

    [Fact]
    public async Task Should_TrackPackage_When_ProductExists()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new TrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackPackageCommand(product.Id.Value, PackageRegistry.Npm, "@accordent/core"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Packages.Should().ContainSingle();
        _products.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_TrackingPackageOnUnknownProduct()
    {
        var useCase = new TrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackPackageCommand(Guid.NewGuid(), PackageRegistry.Npm, "@accordent/core"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_Fail_When_ProductNameIsBlank()
    {
        var useCase = new CreateProductUseCase(_products, new FixedClock(Now));

        var result = await useCase.HandleAsync(new CreateProductCommand("  ", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NameRequired:");
        _products.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_TrackingABlankPackageId()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new TrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackPackageCommand(product.Id.Value, PackageRegistry.Npm, "  "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("PackageId.Empty:");
        _products.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_TrackingTheSamePackageTwice()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        _products.Items.Add(product);
        var useCase = new TrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackPackageCommand(product.Id.Value, PackageRegistry.Npm, "@accordent/core"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.PackageAlreadyTracked:");
        _products.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_TrackRepository_When_ProductExists()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new TrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackRepositoryCommand(product.Id.Value, "Leroy-Florian/Outlet-CLI"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Repositories.Should().ContainSingle();
        _products.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_TrackingRepositoryOnUnknownProduct()
    {
        var useCase = new TrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackRepositoryCommand(Guid.NewGuid(), "Leroy-Florian/Outlet-CLI"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_Fail_When_TrackingTheSameRepositoryTwice()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!);
        _products.Items.Add(product);
        var useCase = new TrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackRepositoryCommand(product.Id.Value, "Leroy-Florian/Outlet-CLI"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.RepositoryAlreadyTracked:");
        _products.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_RepositoryNameIsInvalid()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        var useCase = new TrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new TrackRepositoryCommand(product.Id.Value, "not-a-repo"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("RepositoryName.Invalid:");
    }

    [Fact]
    public async Task Should_CaptureAllTrackedSources_When_ProductHasPackagesAndRepositories()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/react").Value!);
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Accordent").Value!);
        _products.Items.Add(product);

        var downloads = new FakeDownloadSnapshotRepository();
        var repos = new FakeRepositorySnapshotRepository();
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(500L)),
            new FakeRepoStatsClient(Result.Success(new RepoStats(3, 42, 7))),
            downloads,
            repos,
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(4);
        result.Value!.Should().OnlyContain(report => report.Succeeded);
        result.Value!.Select(r => r.Target).Should().Contain("releases:Leroy-Florian/Accordent");
        downloads.Items.Should().HaveCount(2);
        repos.Items.Should().ContainSingle().Which.OpenIssues.Should().Be(3);
    }

    [Fact]
    public async Task Should_ContinueCapturing_When_OneSourceFails()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Accordent").Value!);
        _products.Items.Add(product);

        var downloads = new FakeDownloadSnapshotRepository();
        var repos = new FakeRepositorySnapshotRepository();
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Failure<long>("NpmStats.HttpError: boom")),
            new FakeRepoStatsClient(Result.Success(new RepoStats(1, 2, 3))),
            downloads,
            repos,
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var failed = result.Value!.Should().ContainSingle(r => !r.Succeeded).Subject;
        failed.Error.Should().StartWith("NpmStats.HttpError:");
        downloads.Items.Should().BeEmpty();
        repos.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_ReportFailure_When_PackageCountIsNegative()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        _products.Items.Add(product);

        var downloads = new FakeDownloadSnapshotRepository();
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(-1L)),
            new FakeRepoStatsClient(Result.Success(new RepoStats(0, 0, 0))),
            downloads,
            new FakeRepositorySnapshotRepository(),
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!.Should().ContainSingle().Subject;
        report.Succeeded.Should().BeFalse();
        report.Error.Should().StartWith("DownloadSnapshot.NegativeCount:");
        downloads.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RecordNewReleases_When_CapturingSnapshots()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!);
        _products.Items.Add(product);

        var releases = new FakeReleaseRepository();
        var client = new FakeRepoStatsClient(Result.Success(new RepoStats(0, 0, 0)))
        {
            ReleasesResult = Result.Success<IReadOnlyList<RepoRelease>>(
                [new RepoRelease("v1.0.0", "One", Now)]),
        };
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(1L)),
            client,
            new FakeDownloadSnapshotRepository(),
            new FakeRepositorySnapshotRepository(),
            releases,
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!.Should().ContainSingle(r => r.Target == "releases:Leroy-Florian/Outlet-CLI").Subject;
        report.Succeeded.Should().BeTrue();
        report.Error.Should().BeNull();
        releases.Items.Should().ContainSingle().Which.TagName.Should().Be("v1.0.0");
    }

    [Fact]
    public async Task Should_ReportReleaseSyncFailure_When_ReleaseFetchFails()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!);
        _products.Items.Add(product);

        var releases = new FakeReleaseRepository();
        var client = new FakeRepoStatsClient(Result.Success(new RepoStats(0, 0, 0)))
        {
            ReleasesResult = Result.Failure<IReadOnlyList<RepoRelease>>("GitHubStats.HttpError: boom"),
        };
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(1L)),
            client,
            new FakeDownloadSnapshotRepository(),
            new FakeRepositorySnapshotRepository(),
            releases,
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!.Should().ContainSingle(r => r.Target == "releases:Leroy-Florian/Outlet-CLI").Subject;
        report.Succeeded.Should().BeFalse();
        report.Error.Should().Be("GitHubStats.HttpError: boom");
        releases.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReportFailure_When_RepositoryStatsCannotBeFetched()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!);
        _products.Items.Add(product);

        var repos = new FakeRepositorySnapshotRepository();
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(1L)),
            new FakeRepoStatsClient(Result.Failure<RepoStats>("GitHubStats.HttpError: boom")),
            new FakeDownloadSnapshotRepository(),
            repos,
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!.Should().ContainSingle(r => !r.Succeeded).Subject;
        report.Target.Should().Be("github:Leroy-Florian/Outlet-CLI");
        report.Error.Should().StartWith("GitHubStats.HttpError:");
        repos.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReportFailure_When_RepositoryCountersAreNegative()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!);
        _products.Items.Add(product);

        var repos = new FakeRepositorySnapshotRepository();
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(1L)),
            new FakeRepoStatsClient(Result.Success(new RepoStats(-1, 2, 3))),
            new FakeDownloadSnapshotRepository(),
            repos,
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!.Should().ContainSingle(r => !r.Succeeded).Subject;
        report.Error.Should().StartWith("RepositorySnapshot.NegativeCount:");
        repos.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_CapturingUnknownProduct()
    {
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(1L)),
            new FakeRepoStatsClient(Result.Success(new RepoStats(0, 0, 0))),
            new FakeDownloadSnapshotRepository(),
            new FakeRepositorySnapshotRepository(),
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_StoreLatestVersionOnSnapshots_When_VersionLookupSucceeds()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        _products.Items.Add(product);

        var downloads = new FakeDownloadSnapshotRepository();
        var stats = new FakePackageStatsClient(Result.Success(500L))
        {
            VersionsResult = Result.Success(new PackageVersionInfo("2.0.0", null, 4)),
        };
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            stats,
            new FakeRepoStatsClient(Result.Success(new RepoStats(0, 0, 0))),
            downloads,
            new FakeRepositorySnapshotRepository(),
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        downloads.Items.Should().ContainSingle().Which.LatestVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task Should_StillCaptureSnapshot_When_VersionLookupFails()
    {
        var product = Product.Create("Accordent", null, Now).Value!;
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        _products.Items.Add(product);

        var downloads = new FakeDownloadSnapshotRepository();
        var useCase = new CaptureProductSnapshotsUseCase(
            _products,
            new FakePackageStatsClient(Result.Success(500L)),
            new FakeRepoStatsClient(Result.Success(new RepoStats(0, 0, 0))),
            downloads,
            new FakeRepositorySnapshotRepository(),
            new FakeReleaseRepository(),
            new FixedClock(Now));

        var result = await useCase.HandleAsync(new CaptureProductSnapshotsCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        downloads.Items.Should().ContainSingle().Which.LatestVersion.Should().BeNull();
    }
}
