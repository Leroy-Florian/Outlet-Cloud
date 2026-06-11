using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakePackageStatsClient(Result<long> result) : IPackageStatsClient
{
    public List<(PackageRegistry Registry, string PackageId)> Calls { get; } = [];

    public List<(PackageRegistry Registry, string PackageId)> VersionCalls { get; } = [];

    public Result<PackageVersionInfo> VersionsResult { get; set; } =
        Result.Failure<PackageVersionInfo>("PackageStats.NoVersions: not configured.");

    public Task<Result<long>> GetTotalDownloadsAsync(PackageRegistry registry, PackageId packageId, CancellationToken cancellationToken = default)
    {
        Calls.Add((registry, packageId.Value));
        return Task.FromResult(result);
    }

    public Task<Result<PackageVersionInfo>> GetVersionsAsync(PackageRegistry registry, PackageId packageId, CancellationToken cancellationToken = default)
    {
        VersionCalls.Add((registry, packageId.Value));
        return Task.FromResult(VersionsResult);
    }
}

public sealed class FakeRepoStatsClient(Result<RepoStats> result) : IRepoStatsClient
{
    public List<string> Calls { get; } = [];

    public List<string> ReleaseCalls { get; } = [];

    public Result<IReadOnlyList<RepoRelease>> ReleasesResult { get; set; } =
        Result.Success<IReadOnlyList<RepoRelease>>([]);

    public Task<Result<RepoStats>> GetRepositoryStatsAsync(RepositoryName repository, CancellationToken cancellationToken = default)
    {
        Calls.Add(repository.FullName);
        return Task.FromResult(result);
    }

    public Task<Result<IReadOnlyList<RepoRelease>>> GetReleasesAsync(RepositoryName repository, CancellationToken cancellationToken = default)
    {
        ReleaseCalls.Add(repository.FullName);
        return Task.FromResult(ReleasesResult);
    }
}
