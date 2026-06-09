using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Application.UnitTests.Fakes;

public sealed class FakeSubscriptionRepository : ISubscriptionRepository
{
    private readonly Dictionary<Guid, Subscription> _byAccount = [];

    public int UpdateCount { get; private set; }

    public void Seed(Subscription subscription) => _byAccount[subscription.AccountId.Value] = subscription;

    public Task<Subscription?> GetByAccountAsync(AccountId accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byAccount.GetValueOrDefault(accountId.Value));

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _byAccount[subscription.AccountId.Value] = subscription;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _byAccount[subscription.AccountId.Value] = subscription;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
