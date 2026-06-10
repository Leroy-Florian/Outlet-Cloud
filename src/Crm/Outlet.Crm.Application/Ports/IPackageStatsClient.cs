using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Ports;

/// <summary>
/// SECONDARY PORT — package registry download statistics. One adapter per registry
/// (NuGet azuresearch, npm downloads API…) lives in Infrastructure.
/// Note : NuGet renvoie un cumul total ; npm renvoie un volume glissant
/// (30 derniers jours), la sémantique du chiffre dépend donc du registre.
/// </summary>
/// <summary>
/// Latest published version of a package. <c>PublishedAt</c> is null when the
/// registry does not expose publication dates (NuGet flat-container). Known v1
/// limitation, kept honest on purpose: neither npm nor NuGet expose per-version
/// download splits through public point queries, so adoption is approximated by
/// release markers (version changes between snapshots), never by faked splits.
/// </summary>
public sealed record PackageVersionInfo(string LatestVersion, DateTime? PublishedAt, int VersionCount);

public interface IPackageStatsClient
{
    Task<Result<long>> GetTotalDownloadsAsync(PackageRegistry registry, PackageId packageId, CancellationToken cancellationToken = default);

    Task<Result<PackageVersionInfo>> GetVersionsAsync(PackageRegistry registry, PackageId packageId, CancellationToken cancellationToken = default);
}
