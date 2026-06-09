using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Subscriptions;

/// <summary>
/// The server-side authorization heart: resolves the <see cref="Entitlements"/> currently
/// in force for an account. Every cloud use case that gates a feature goes through here
/// instead of branching on status, so the trial/paid distinction lives in exactly one place.
///
/// Performs LAZY expiry: a trialing subscription whose window has elapsed is transitioned to
/// Suspended (and persisted) the first time anyone asks — no scheduler required for correctness
/// (a background job still drives the courtesy e-mails / purge timeline).
/// </summary>
public sealed class SubscriptionEntitlementResolver(ISubscriptionRepository subscriptions, ICurrentDateTimeProvider clock)
{
    public async Task<Entitlements> ResolveAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptions.GetByAccountAsync(accountId, cancellationToken);
        if (subscription is null)
            return Entitlements.None;

        if (subscription.Status == SubscriptionStatus.Trialing
            && subscription.Trial?.HasElapsedAsOf(clock.Today) == true)
        {
            subscription.ExpireTrial(clock.Today);
            await subscriptions.UpdateAsync(subscription, cancellationToken);
        }

        return subscription.ResolveEntitlements();
    }
}
