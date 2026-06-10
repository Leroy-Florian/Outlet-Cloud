using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetRepositoryHistoryQuery(Guid ProductId, string Repository);

/// <summary>Returns the GitHub snapshot history of one tracked repository.</summary>
public sealed class GetRepositoryHistoryUseCase(IRepositorySnapshotRepository snapshots)
    : IUseCase<GetRepositoryHistoryQuery, IReadOnlyList<RepositorySnapshot>>
{
    public async Task<Result<IReadOnlyList<RepositorySnapshot>>> HandleAsync(
        GetRepositoryHistoryQuery command,
        CancellationToken cancellationToken = default)
    {
        var repository = RepositoryName.Create(command.Repository);
        if (repository.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RepositorySnapshot>>(repository.Error!);
        }

        var history = await snapshots.ListByRepositoryAsync(
            new ProductId(command.ProductId), repository.Value!, cancellationToken);

        return Result.Success(history);
    }
}
