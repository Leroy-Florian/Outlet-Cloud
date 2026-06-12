using Microsoft.EntityFrameworkCore;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IOrganizationRepository"/>.</summary>
public sealed class EfOrganizationRepository(CloudDbContext db) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Organizations
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public Task<bool> ExistsWithSlugAsync(OrganizationSlug slug, CancellationToken cancellationToken = default) =>
        db.Organizations.AnyAsync(o => o.Slug == slug.Value, cancellationToken);

    public async Task<IReadOnlyList<Organization>> ListForMemberAsync(MemberUserId userId, CancellationToken cancellationToken = default)
    {
        var records = await db.Organizations
            .Include(o => o.Members)
            .Where(o => o.Members.Any(m => m.UserId == userId.Value))
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task<Organization?> GetBySlugAsync(OrganizationSlug slug, CancellationToken cancellationToken = default)
    {
        var record = await db.Organizations
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Slug == slug.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        db.Organizations.Add(ToRecord(organization));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var record = await db.Organizations
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == organization.Id.Value, cancellationToken);

        if (record is null)
        {
            db.Organizations.Add(ToRecord(organization));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        record.Slug = organization.Slug.Value;
        record.Name = organization.Name.Value;
        record.RegistryVisibility = organization.RegistryVisibility;

        db.OrganizationMembers.RemoveRange(record.Members);
        record.Members = [.. organization.Memberships.Select(m => ToRecord(record.Id, m))];

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Organization ToDomain(OrganizationRecord record) =>
        Organization.Restore(
            OrganizationId.From(record.Id),
            OrganizationSlug.From(record.Slug),
            OrganizationName.From(record.Name),
            record.RegistryVisibility,
            [.. record.Members.Select(m => (MemberUserId.From(m.UserId), m.Role))]);

    private static OrganizationRecord ToRecord(Organization organization) =>
        new()
        {
            Id = organization.Id.Value,
            Slug = organization.Slug.Value,
            Name = organization.Name.Value,
            RegistryVisibility = organization.RegistryVisibility,
            Members = [.. organization.Memberships.Select(m => ToRecord(organization.Id.Value, m))],
        };

    private static MembershipRecord ToRecord(Guid organizationId, Membership membership) =>
        new()
        {
            OrganizationId = organizationId,
            UserId = membership.Id.Value,
            Role = membership.Role,
        };
}
