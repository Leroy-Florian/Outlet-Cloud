using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Analytics;

public sealed record CaptureProductSnapshotsCommand(Guid ProductId);

public sealed record SnapshotCaptureReport(string Target, bool Succeeded, string? Error);

/// <summary>
/// Capture en une passe les snapshots de tous les packages (NuGet/npm)
/// et repositories GitHub suivis par un produit. Best-effort : une source
/// en échec n'empêche pas les autres d'être capturées.
/// </summary>
public sealed class CaptureProductSnapshotsUseCase(
    IProductRepository products,
    IPackageStatsClient packageStats,
    IRepoStatsClient repoStats,
    IDownloadSnapshotRepository downloadSnapshots,
    IRepositorySnapshotRepository repositorySnapshots,
    ICurrentDateTimeProvider clock)
    : IUseCase<CaptureProductSnapshotsCommand, IReadOnlyList<SnapshotCaptureReport>>
{
    public async Task<Result<IReadOnlyList<SnapshotCaptureReport>>> HandleAsync(
        CaptureProductSnapshotsCommand command,
        CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<IReadOnlyList<SnapshotCaptureReport>>(ProductErrors.NotFound(productId));
        }

        List<SnapshotCaptureReport> reports = [];

        foreach (var package in product.Packages)
        {
            var downloads = await packageStats.GetTotalDownloadsAsync(package.Registry, package.PackageId, cancellationToken);
            if (downloads.IsFailure)
            {
                reports.Add(new SnapshotCaptureReport($"{package.Registry}:{package.PackageId.Value}", false, downloads.Error));
                continue;
            }

            var snapshot = DownloadSnapshot.Create(productId, package.Registry, package.PackageId, downloads.Value, clock.UtcNow);
            if (snapshot.IsFailure)
            {
                reports.Add(new SnapshotCaptureReport($"{package.Registry}:{package.PackageId.Value}", false, snapshot.Error));
                continue;
            }

            await downloadSnapshots.AddAsync(snapshot.Value!, cancellationToken);
            reports.Add(new SnapshotCaptureReport($"{package.Registry}:{package.PackageId.Value}", true, null));
        }

        foreach (var tracked in product.Repositories)
        {
            var stats = await repoStats.GetRepositoryStatsAsync(tracked.Repository, cancellationToken);
            if (stats.IsFailure)
            {
                reports.Add(new SnapshotCaptureReport($"github:{tracked.Repository.FullName}", false, stats.Error));
                continue;
            }

            var snapshot = RepositorySnapshot.Create(
                productId, tracked.Repository, stats.Value!.OpenIssues, stats.Value.Stars, stats.Value.Forks, clock.UtcNow);
            if (snapshot.IsFailure)
            {
                reports.Add(new SnapshotCaptureReport($"github:{tracked.Repository.FullName}", false, snapshot.Error));
                continue;
            }

            await repositorySnapshots.AddAsync(snapshot.Value!, cancellationToken);
            reports.Add(new SnapshotCaptureReport($"github:{tracked.Repository.FullName}", true, null));
        }

        return Result.Success<IReadOnlyList<SnapshotCaptureReport>>(reports);
    }
}
