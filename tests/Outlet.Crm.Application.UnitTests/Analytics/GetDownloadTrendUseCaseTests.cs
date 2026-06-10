using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class GetDownloadTrendUseCaseTests
{
    private static readonly DateTime Day1 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_ReturnTrendWithDeltas_When_HistoryExists()
    {
        var repository = new FakeDownloadSnapshotRepository();
        var productId = ProductId.New();
        var packageId = PackageId.Create("outlet.cli").Value!;
        repository.Items.Add(DownloadSnapshot.Create(productId, PackageRegistry.NuGet, packageId, 100, Day1).Value!);
        repository.Items.Add(DownloadSnapshot.Create(productId, PackageRegistry.NuGet, packageId, 130, Day1.AddDays(1)).Value!);
        var useCase = new GetDownloadTrendUseCase(repository);

        var result = await useCase.HandleAsync(
            new GetDownloadTrendQuery(productId.Value, PackageRegistry.NuGet, "outlet.cli"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(p => p.Delta).Should().Equal(0, 30);
    }

    [Fact]
    public async Task Should_Fail_When_PackageIdIsEmpty()
    {
        var useCase = new GetDownloadTrendUseCase(new FakeDownloadSnapshotRepository());

        var result = await useCase.HandleAsync(
            new GetDownloadTrendQuery(Guid.NewGuid(), PackageRegistry.Npm, ""), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("PackageId.Empty:");
    }
}
