using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class CaptureDownloadSnapshotUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeDownloadSnapshotRepository _repository = new();
    private readonly FakeProductRepository _products = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public CaptureDownloadSnapshotUseCaseTests()
    {
        _product.TrackPackage(PackageRegistry.NuGet, PackageId.Create("outlet.cli").Value!);
        _products.Items.Add(_product);
    }

    private CaptureDownloadSnapshotUseCase UseCase(Result<long> statsResult) =>
        new(new FakePackageStatsClient(statsResult), _products, _repository, new FixedClock(Now));

    [Fact]
    public async Task Should_StoreSnapshot_When_PackageIsTracked()
    {
        var result = await UseCase(Result.Success(1234L)).HandleAsync(
            new CaptureDownloadSnapshotCommand(_product.Id.Value, PackageRegistry.NuGet, "Outlet.Cli"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1234);
        var snapshot = _repository.Items.Should().ContainSingle().Subject;
        snapshot.PackageId.Value.Should().Be("outlet.cli");
        snapshot.ProductId.Should().Be(_product.Id);
        snapshot.CapturedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_Fail_When_PackageIsNotTracked()
    {
        var result = await UseCase(Result.Success(1L)).HandleAsync(
            new CaptureDownloadSnapshotCommand(_product.Id.Value, PackageRegistry.Npm, "outlet.cli"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.PackageNotTracked:");
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await UseCase(Result.Success(1L)).HandleAsync(
            new CaptureDownloadSnapshotCommand(Guid.NewGuid(), PackageRegistry.NuGet, "outlet.cli"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_PropagateError_When_RegistryFails()
    {
        const string error = "NuGetStats.HttpError: boom";

        var result = await UseCase(Result.Failure<long>(error)).HandleAsync(
            new CaptureDownloadSnapshotCommand(_product.Id.Value, PackageRegistry.NuGet, "outlet.cli"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        _repository.Items.Should().BeEmpty();
    }
}
