using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Ports;

public sealed record RepoStats(int OpenIssues, int Stars, int Forks);

/// <summary>One published release as reported by the repository host.</summary>
public sealed record RepoRelease(string TagName, string? Name, DateTime PublishedAt);

/// <summary>SECONDARY PORT — the GitHub repository API; the HTTP adapter lives in Infrastructure.</summary>
public interface IRepoStatsClient
{
    Task<Result<RepoStats>> GetRepositoryStatsAsync(RepositoryName repository, CancellationToken cancellationToken = default);

    /// <summary>Latest published releases of the repository, newest first.</summary>
    Task<Result<IReadOnlyList<RepoRelease>>> GetReleasesAsync(RepositoryName repository, CancellationToken cancellationToken = default);
}
