namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>
/// Lifecycle state of a <see cref="Subscription"/>. The whole product runs on a
/// single, frictionless trial path — there is no permanent free tier:
///
///   Trialing ──convert──▶ Active
///       │
///       └──expire──▶ Suspended (read-only, data kept N days)
///                        │
///                        ├──pay──▶ Active   (instant reactivation)
///                        └──purge──▶ Expired (data purged after notice)
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Full Pro access during the time-limited trial.</summary>
    Trialing = 0,

    /// <summary>Paying customer with full Pro access.</summary>
    Active = 1,

    /// <summary>Trial elapsed (or payment lapsed): read-only, data retained pending payment.</summary>
    Suspended = 2,

    /// <summary>Retention window elapsed: data purged, account closed.</summary>
    Expired = 3,
}
