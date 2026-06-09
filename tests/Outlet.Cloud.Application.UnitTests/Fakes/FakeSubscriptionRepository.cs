using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Application.UnitTests.Fakes;

public sealed class FakeSubscriptionRepository : ISubscriptionRepository
{
    private readonly Dictionary<Guid, Subscription> _byOrganization = [];

    public int UpdateCount { get; private set; }

    public void Seed(Subscription subscription) => _byOrganization[subscription.OrganizationId.Value] = subscription;

    public Task<Subscription?> GetByOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byOrganization.GetValueOrDefault(organizationId.Value));

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _byOrganization[subscription.OrganizationId.Value] = subscription;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _byOrganization[subscription.OrganizationId.Value] = subscription;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
