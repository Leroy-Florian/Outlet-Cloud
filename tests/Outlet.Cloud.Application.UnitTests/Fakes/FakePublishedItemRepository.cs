using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;

namespace Outlet.Cloud.Application.UnitTests.Fakes;

public sealed class FakePublishedItemRepository : IPublishedItemRepository
{
    private readonly Dictionary<Guid, PublishedItem> _byId = [];

    public Task UpsertAsync(PublishedItem item, CancellationToken cancellationToken = default)
    {
        _byId[item.Id.Value] = item;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PublishedItem>> ListForOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PublishedItem>>([.. _byId.Values.Where(i => i.OrganizationId == organizationId)]);

    public Task<PublishedItem?> GetAsync(OrganizationId organizationId, RegistryItemName name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(i => i.OrganizationId == organizationId && i.Name == name));
}
