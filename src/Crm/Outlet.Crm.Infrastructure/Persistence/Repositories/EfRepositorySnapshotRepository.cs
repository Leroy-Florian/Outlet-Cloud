using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IRepositorySnapshotRepository"/>.</summary>
public sealed class EfRepositorySnapshotRepository(CrmDbContext db) : IRepositorySnapshotRepository
{
    public async Task<IReadOnlyList<RepositorySnapshot>> ListByRepositoryAsync(
        ProductId productId,
        RepositoryName repository,
        CancellationToken cancellationToken = default) =>
        await db.RepositorySnapshots
            .AsNoTracking()
            .Where(s => s.ProductId == productId && s.Repository == repository)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RepositorySnapshot>> ListByProductAsync(
        ProductId productId,
        CancellationToken cancellationToken = default) =>
        await db.RepositorySnapshots
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RepositorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        db.RepositorySnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);
    }
}
