using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Ports;

public sealed record RepoStats(int OpenIssues, int Stars, int Forks);

/// <summary>SECONDARY PORT — the GitHub repository API; the HTTP adapter lives in Infrastructure.</summary>
public interface IRepoStatsClient
{
    Task<Result<RepoStats>> GetRepositoryStatsAsync(RepositoryName repository, CancellationToken cancellationToken = default);
}
