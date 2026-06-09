using Microsoft.EntityFrameworkCore;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IPublishedItemRepository"/>.</summary>
public sealed class EfPublishedItemRepository(CloudDbContext db) : IPublishedItemRepository
{
    public async Task UpsertAsync(PublishedItem item, CancellationToken cancellationToken = default)
    {
        var record = await db.PublishedItems
            .Include(i => i.Files)
            .FirstOrDefaultAsync(i => i.Id == item.Id.Value, cancellationToken);

        if (record is null)
        {
            db.PublishedItems.Add(ToRecord(item));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        record.Name = item.Name.Value;
        record.ManifestJson = item.ManifestJson;
        db.RemoveRange(record.Files);
        record.Files = [.. item.Files.Select(f => new PublishedFileRecord { PublishedItemId = record.Id, Path = f.Path, Content = f.Content })];

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublishedItem>> ListForOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var records = await db.PublishedItems
            .Include(i => i.Files)
            .Where(i => i.OrganizationId == organizationId.Value)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task<PublishedItem?> GetAsync(OrganizationId organizationId, RegistryItemName name, CancellationToken cancellationToken = default)
    {
        var record = await db.PublishedItems
            .Include(i => i.Files)
            .FirstOrDefaultAsync(i => i.OrganizationId == organizationId.Value && i.Name == name.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    private static PublishedItem ToDomain(PublishedItemRecord record) =>
        PublishedItem.Restore(
            PublishedItemId.From(record.Id),
            OrganizationId.From(record.OrganizationId),
            RegistryItemName.From(record.Name),
            record.ManifestJson,
            [.. record.Files.Select(f => PublishedFile.From(f.Path, f.Content))]);

    private static PublishedItemRecord ToRecord(PublishedItem item) =>
        new()
        {
            Id = item.Id.Value,
            OrganizationId = item.OrganizationId.Value,
            Name = item.Name.Value,
            ManifestJson = item.ManifestJson,
            Files = [.. item.Files.Select(f => new PublishedFileRecord { PublishedItemId = item.Id.Value, Path = f.Path, Content = f.Content })],
        };
}
