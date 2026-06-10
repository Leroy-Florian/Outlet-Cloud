using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record GetDownloadTrendQuery(Guid ProductId, PackageRegistry Registry, string PackageId);

/// <summary>Returns the download history of a package as per-snapshot deltas.</summary>
public sealed class GetDownloadTrendUseCase(IDownloadSnapshotRepository snapshots)
    : IUseCase<GetDownloadTrendQuery, IReadOnlyList<DownloadTrendPoint>>
{
    public async Task<Result<IReadOnlyList<DownloadTrendPoint>>> HandleAsync(
        GetDownloadTrendQuery command,
        CancellationToken cancellationToken = default)
    {
        var packageId = PackageId.Create(command.PackageId);
        if (packageId.IsFailure)
        {
            return Result.Failure<IReadOnlyList<DownloadTrendPoint>>(packageId.Error!);
        }

        var history = await snapshots.ListByPackageAsync(
            new ProductId(command.ProductId), command.Registry, packageId.Value!, cancellationToken);

        return Result.Success(DownloadTrend.FromSnapshots(history));
    }
}
