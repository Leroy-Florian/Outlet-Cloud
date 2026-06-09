using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>
/// VALUE OBJECT — what an account is allowed to do server-side, RIGHT NOW. This is the
/// single currency of authorization: every cloud use case asks the subscription for its
/// entitlements rather than branching on <see cref="SubscriptionStatus"/> directly.
///
/// Trial and paid customers flow through the same resolution
/// (<see cref="Subscription.ResolveEntitlements"/>); only the values differ. When real
/// billing arrives, new plans add values here — the call sites never change.
/// </summary>
public sealed class Entitlements : ValueObject
{
    /// <summary>May push new/updated items to a private registry.</summary>
    public bool CanPublishPrivateItems { get; }

    /// <summary>May read/export the existing private registry (kept true while suspended).</summary>
    public bool CanReadPrivateRegistry { get; }

    /// <summary>Maximum number of items hostable in the private registry.</summary>
    public int MaxPrivateItems { get; }

    /// <summary>Access to usage analytics.</summary>
    public bool Analytics { get; }

    private Entitlements(bool canPublishPrivateItems, bool canReadPrivateRegistry, int maxPrivateItems, bool analytics)
    {
        CanPublishPrivateItems = canPublishPrivateItems;
        CanReadPrivateRegistry = canReadPrivateRegistry;
        MaxPrivateItems = maxPrivateItems;
        Analytics = analytics;
    }

    /// <summary>Full feature set for a plan (trial and Active share this for the Pro tier).</summary>
    public static Entitlements For(PlanTier plan) => plan switch
    {
        PlanTier.Pro => new Entitlements(canPublishPrivateItems: true, canReadPrivateRegistry: true, maxPrivateItems: 100, analytics: true),
        _ => None,
    };

    /// <summary>Suspended accounts: consult/export only, no new pushes.</summary>
    public static readonly Entitlements ReadOnly =
        new(canPublishPrivateItems: false, canReadPrivateRegistry: true, maxPrivateItems: 0, analytics: false);

    /// <summary>Expired/unknown accounts: nothing.</summary>
    public static readonly Entitlements None =
        new(canPublishPrivateItems: false, canReadPrivateRegistry: false, maxPrivateItems: 0, analytics: false);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CanPublishPrivateItems;
        yield return CanReadPrivateRegistry;
        yield return MaxPrivateItems;
        yield return Analytics;
    }
}
