using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Infrastructure.PackageStats;

/// <summary>Routes stats requests to the adapter matching the registry.</summary>
public sealed class PackageStatsClient(NuGetStatsHttpClient nuGet, NpmStatsHttpClient npm) : IPackageStatsClient
{
    public Task<Result<long>> GetTotalDownloadsAsync(PackageRegistry registry, PackageId packageId, CancellationToken cancellationToken = default) =>
        registry switch
        {
            PackageRegistry.NuGet => nuGet.GetTotalDownloadsAsync(packageId, cancellationToken),
            PackageRegistry.Npm => npm.GetTotalDownloadsAsync(packageId, cancellationToken),
            _ => Task.FromResult(Result.Failure<long>(
                $"PackageStats.UnknownRegistry: Unknown package registry '{registry}'.")),
        };
}
