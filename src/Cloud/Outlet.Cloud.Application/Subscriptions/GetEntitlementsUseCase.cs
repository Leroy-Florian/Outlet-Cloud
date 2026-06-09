using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Subscriptions;

/// <summary>Query: the current entitlement snapshot for an account.</summary>
public sealed record GetEntitlementsQuery(Guid AccountId);

/// <summary>
/// What `outlet whoami` surfaces and what the API can echo back: plan, status, trial days left,
/// and the resolved entitlement flags. This is a READ of the authorization state — it triggers
/// lazy trial expiry through <see cref="SubscriptionEntitlementResolver"/> so the snapshot is
/// always honest.
/// </summary>
public sealed record EntitlementsView(
    bool HasSubscription,
    string Status,
    string Plan,
    int TrialDaysRemaining,
    bool CanPublishPrivateItems,
    bool CanReadPrivateRegistry,
    int MaxPrivateItems,
    bool Analytics);

public sealed class GetEntitlementsUseCase(
    ISubscriptionRepository subscriptions,
    SubscriptionEntitlementResolver resolver,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetEntitlementsQuery, EntitlementsView>
{
    public async Task<Result<EntitlementsView>> HandleAsync(GetEntitlementsQuery query, CancellationToken cancellationToken = default)
    {
        var accountResult = Guard.TryBuild(() => AccountId.From(query.AccountId), "Account id is invalid.");
        if (accountResult.IsFailure)
            return Result<EntitlementsView>.Failure(accountResult.Error!);

        // Resolve first so any elapsed trial is transitioned + persisted before we read it back.
        var entitlements = await resolver.ResolveAsync(accountResult.Value!, cancellationToken);
        var subscription = await subscriptions.GetByAccountAsync(accountResult.Value!, cancellationToken);

        if (subscription is null)
            return Result<EntitlementsView>.Success(new EntitlementsView(
                HasSubscription: false,
                Status: nameof(SubscriptionStatus.Expired),
                Plan: nameof(PlanTier.Pro),
                TrialDaysRemaining: 0,
                CanPublishPrivateItems: entitlements.CanPublishPrivateItems,
                CanReadPrivateRegistry: entitlements.CanReadPrivateRegistry,
                MaxPrivateItems: entitlements.MaxPrivateItems,
                Analytics: entitlements.Analytics));

        return Result<EntitlementsView>.Success(new EntitlementsView(
            HasSubscription: true,
            Status: subscription.Status.ToString(),
            Plan: subscription.Plan.ToString(),
            TrialDaysRemaining: subscription.TrialDaysRemainingAsOf(clock.Today),
            CanPublishPrivateItems: entitlements.CanPublishPrivateItems,
            CanReadPrivateRegistry: entitlements.CanReadPrivateRegistry,
            MaxPrivateItems: entitlements.MaxPrivateItems,
            Analytics: entitlements.Analytics));
    }
}
