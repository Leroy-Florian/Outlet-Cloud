using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakePackageStatsClient(Result<long> result) : IPackageStatsClient
{
    public List<(PackageRegistry Registry, string PackageId)> Calls { get; } = [];

    public Task<Result<long>> GetTotalDownloadsAsync(PackageRegistry registry, PackageId packageId, CancellationToken cancellationToken = default)
    {
        Calls.Add((registry, packageId.Value));
        return Task.FromResult(result);
    }
}

public sealed class FakeRepoStatsClient(Result<RepoStats> result) : IRepoStatsClient
{
    public List<string> Calls { get; } = [];

    public Task<Result<RepoStats>> GetRepositoryStatsAsync(RepositoryName repository, CancellationToken cancellationToken = default)
    {
        Calls.Add(repository.FullName);
        return Task.FromResult(result);
    }
}
