using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IDownloadSnapshotRepository"/>.</summary>
public sealed class EfDownloadSnapshotRepository(CrmDbContext db) : IDownloadSnapshotRepository
{
    public async Task<IReadOnlyList<DownloadSnapshot>> ListByPackageAsync(
        ProductId productId,
        PackageRegistry registry,
        PackageId packageId,
        CancellationToken cancellationToken = default) =>
        await db.DownloadSnapshots
            .AsNoTracking()
            .Where(s => s.ProductId == productId && s.Registry == registry && s.PackageId == packageId)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DownloadSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        db.DownloadSnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);
    }
}
