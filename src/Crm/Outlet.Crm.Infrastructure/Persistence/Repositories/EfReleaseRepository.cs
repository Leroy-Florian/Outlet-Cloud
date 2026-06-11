using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Releases;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IReleaseRepository"/>.</summary>
public sealed class EfReleaseRepository(CrmDbContext db) : IReleaseRepository
{
    public async Task<IReadOnlyList<ReleaseRecord>> ListByProductAsync(
        ProductId productId,
        CancellationToken cancellationToken = default) =>
        await db.Releases
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.PublishedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        ProductId productId,
        RepositoryName repository,
        string tagName,
        CancellationToken cancellationToken = default) =>
        await db.Releases
            .AsNoTracking()
            .AnyAsync(r => r.ProductId == productId && r.Repository == repository && r.TagName == tagName, cancellationToken);

    public async Task AddAsync(ReleaseRecord release, CancellationToken cancellationToken = default)
    {
        db.Releases.Add(release);
        await db.SaveChangesAsync(cancellationToken);
    }
}
