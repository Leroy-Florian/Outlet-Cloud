using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="Subscription"/> aggregates (one per account).</summary>
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByAccountAsync(AccountId accountId, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
