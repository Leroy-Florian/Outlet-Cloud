using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IProspectRepository"/>.</summary>
public sealed class EfProspectRepository(CrmDbContext db) : IProspectRepository
{
    public Task<Prospect?> GetByIdAsync(ProspectId id, CancellationToken cancellationToken = default) =>
        db.Prospects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Prospects.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(Prospect prospect, CancellationToken cancellationToken = default)
    {
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Prospect prospect, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
