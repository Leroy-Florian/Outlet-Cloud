using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;

namespace Outlet.Cloud.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="PublishedItem"/> aggregates (an org's private registry content).</summary>
public interface IPublishedItemRepository
{
    /// <summary>Inserts a new item or replaces the existing one with the same id.</summary>
    Task UpsertAsync(PublishedItem item, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublishedItem>> ListForOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken = default);

    Task<PublishedItem?> GetAsync(OrganizationId organizationId, RegistryItemName name, CancellationToken cancellationToken = default);
}
