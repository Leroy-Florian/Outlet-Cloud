using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record CaptureDownloadSnapshotCommand(Guid ProductId, PackageRegistry Registry, string PackageId);

/// <summary>Captures the current download count of one tracked package.</summary>
public sealed class CaptureDownloadSnapshotUseCase(
    IPackageStatsClient packageStats,
    IProductRepository products,
    IDownloadSnapshotRepository snapshots,
    ICurrentDateTimeProvider clock)
    : IUseCase<CaptureDownloadSnapshotCommand, long>
{
    public async Task<Result<long>> HandleAsync(CaptureDownloadSnapshotCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<long>(ProductErrors.NotFound(productId));
        }

        var packageId = PackageId.Create(command.PackageId);
        if (packageId.IsFailure)
        {
            return Result.Failure<long>(packageId.Error!);
        }

        if (!product.IsTracking(command.Registry, packageId.Value!))
        {
            return Result.Failure<long>(ProductErrors.PackageNotTracked(command.Registry, packageId.Value!));
        }

        var totalDownloads = await packageStats.GetTotalDownloadsAsync(command.Registry, packageId.Value!, cancellationToken);
        if (totalDownloads.IsFailure)
        {
            return totalDownloads;
        }

        // Best-effort version lookup: a failing version endpoint never blocks the capture.
        var versions = await packageStats.GetVersionsAsync(command.Registry, packageId.Value!, cancellationToken);
        var latestVersion = versions.IsSuccess ? versions.Value!.LatestVersion : null;

        var snapshot = DownloadSnapshot.Create(productId, command.Registry, packageId.Value!, totalDownloads.Value, clock.UtcNow, latestVersion);
        if (snapshot.IsFailure)
        {
            return Result.Failure<long>(snapshot.Error!);
        }

        await snapshots.AddAsync(snapshot.Value!, cancellationToken);

        return Result.Success(totalDownloads.Value);
    }
}
