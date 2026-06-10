using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Organizations;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IOrganizationRepository"/>.</summary>
public sealed class EfOrganizationRepository(CrmDbContext db) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) =>
        db.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Organization>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);
    }
}
