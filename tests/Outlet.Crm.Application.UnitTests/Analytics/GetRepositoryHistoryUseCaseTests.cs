using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Analytics;

public sealed class GetRepositoryHistoryUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeRepositorySnapshotRepository _snapshots = new();

    [Fact]
    public async Task Should_ReturnHistory_When_RepositoryIsValid()
    {
        var productId = ProductId.New();
        var repository = RepositoryName.Create("Leroy-Florian/Outlet-CLI").Value!;
        _snapshots.Items.Add(RepositorySnapshot.Create(productId, repository, 1, 2, 3, Now).Value!);
        _snapshots.Items.Add(RepositorySnapshot.Create(ProductId.New(), repository, 9, 9, 9, Now).Value!);
        var useCase = new GetRepositoryHistoryUseCase(_snapshots);

        var result = await useCase.HandleAsync(
            new GetRepositoryHistoryQuery(productId.Value, "Leroy-Florian/Outlet-CLI"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var snapshot = result.Value!.Should().ContainSingle().Subject;
        snapshot.ProductId.Should().Be(productId);
        snapshot.Stars.Should().Be(2);
    }

    [Fact]
    public async Task Should_Fail_When_RepositoryNameIsInvalid()
    {
        var useCase = new GetRepositoryHistoryUseCase(_snapshots);

        var result = await useCase.HandleAsync(
            new GetRepositoryHistoryQuery(Guid.NewGuid(), "not-a-repo"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("RepositoryName.Invalid:");
    }
}
