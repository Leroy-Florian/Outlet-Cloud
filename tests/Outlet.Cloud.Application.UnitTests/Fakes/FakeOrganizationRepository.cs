using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Application.UnitTests.Fakes;

public sealed class FakeOrganizationRepository : IOrganizationRepository
{
    private readonly Dictionary<Guid, Organization> _byId = [];

    public void Seed(Organization organization) => _byId[organization.Id.Value] = organization;

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Organization?>(_byId.GetValueOrDefault(id.Value));

    public Task<bool> ExistsWithSlugAsync(OrganizationSlug slug, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.Any(o => o.Slug == slug));

    public Task<IReadOnlyList<Organization>> ListForMemberAsync(MemberUserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Organization>>([.. _byId.Values.Where(o => o.Memberships.Any(m => m.Id == userId))]);

    public Task<Organization?> GetBySlugAsync(OrganizationSlug slug, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(o => o.Slug == slug));

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _byId[organization.Id.Value] = organization;
        return Task.CompletedTask;
    }

    public int UpdateCount { get; private set; }

    public Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        _byId[organization.Id.Value] = organization;
        return Task.CompletedTask;
    }
}
