using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>
/// AGGREGATE ROOT — an organization's entitlement to Outlet Cloud, modelled as a
/// state machine over <see cref="SubscriptionStatus"/>. "Trialing" is a first-class
/// state, not a throwaway flag: trial and paid plans share one decision path
/// (<see cref="ResolveEntitlements"/>), so introducing real billing later changes
/// values, never call sites.
///
/// Transitions (each enforces its source state and raises a past-tense event):
///   CreateTrial → Trialing
///   Convert     : Trialing → Active
///   ExpireTrial : Trialing → Suspended (only once the trial window has elapsed)
///   Reactivate  : Suspended → Active
///   Purge       : Suspended → Expired
///
/// References the organization by <see cref="Organizations.OrganizationId"/> only.
/// </summary>
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    public OrganizationId OrganizationId { get; }
    public PlanTier Plan { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public TrialPeriod? Trial { get; private set; }

    private Subscription(SubscriptionId id, OrganizationId organizationId, PlanTier plan, SubscriptionStatus status, TrialPeriod? trial)
        : base(id)
    {
        OrganizationId = organizationId;
        Plan = plan;
        Status = status;
        Trial = trial;
    }

    /// <summary>Starts an organization's frictionless Pro trial.</summary>
    public static Result<Subscription> CreateTrial(SubscriptionId id, OrganizationId organizationId, TrialPeriod trial)
    {
        var subscription = new Subscription(id, organizationId, PlanTier.Pro, SubscriptionStatus.Trialing, trial);
        subscription.RaiseDomainEvent(new SubscriptionTrialStartedEvent(id, organizationId, trial));

        return Result<Subscription>.Success(subscription);
    }

    /// <summary>Rehydrates a subscription from TRUSTED persistence without events or guards.</summary>
    public static Subscription Restore(
        SubscriptionId id,
        OrganizationId organizationId,
        PlanTier plan,
        SubscriptionStatus status,
        TrialPeriod? trial) =>
        new(id, organizationId, plan, status, trial);

    /// <summary>Trial → Active: the customer converts to a paying plan.</summary>
    public Result Convert(PlanTier plan)
    {
        if (Status == SubscriptionStatus.Active)
            return Result.Success();

        if (Status != SubscriptionStatus.Trialing)
            return Result.Failure("Only a trialing subscription can be converted.");

        Plan = plan;
        Status = SubscriptionStatus.Active;
        RaiseDomainEvent(new SubscriptionConvertedEvent(Id, OrganizationId, plan));

        return Result.Success();
    }

    /// <summary>Trial → Suspended: the trial window has elapsed; the account goes read-only.</summary>
    public Result ExpireTrial(DateOnly asOf)
    {
        if (Status != SubscriptionStatus.Trialing)
            return Result.Failure("Only a trialing subscription can expire into suspension.");

        if (Trial is null || !Trial.HasElapsedAsOf(asOf))
            return Result.Failure("The trial has not elapsed yet.");

        Status = SubscriptionStatus.Suspended;
        RaiseDomainEvent(new SubscriptionSuspendedEvent(Id, OrganizationId));

        return Result.Success();
    }

    /// <summary>Suspended → Active: payment received; access is restored instantly.</summary>
    public Result Reactivate(PlanTier plan)
    {
        if (Status != SubscriptionStatus.Suspended)
            return Result.Failure("Only a suspended subscription can be reactivated.");

        Plan = plan;
        Status = SubscriptionStatus.Active;
        RaiseDomainEvent(new SubscriptionReactivatedEvent(Id, OrganizationId, plan));

        return Result.Success();
    }

    /// <summary>Suspended → Expired: the retention window elapsed; the account is closed.</summary>
    public Result Purge()
    {
        if (Status != SubscriptionStatus.Suspended)
            return Result.Failure("Only a suspended subscription can be purged.");

        Status = SubscriptionStatus.Expired;
        RaiseDomainEvent(new SubscriptionExpiredEvent(Id, OrganizationId));

        return Result.Success();
    }

    /// <summary>The single source of truth for "what may this account do now?".</summary>
    public Entitlements ResolveEntitlements() => Status switch
    {
        SubscriptionStatus.Trialing => Entitlements.For(Plan),
        SubscriptionStatus.Active => Entitlements.For(Plan),
        SubscriptionStatus.Suspended => Entitlements.ReadOnly,
        _ => Entitlements.None,
    };

    /// <summary>Whole trial days left as of <paramref name="today"/> (0 when not trialing).</summary>
    public int TrialDaysRemainingAsOf(DateOnly today) =>
        Status == SubscriptionStatus.Trialing && Trial is not null ? Trial.DaysRemainingAsOf(today) : 0;
}
