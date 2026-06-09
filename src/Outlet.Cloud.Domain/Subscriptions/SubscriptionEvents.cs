using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>Raised when an account starts its frictionless Pro trial.</summary>
public sealed record SubscriptionTrialStartedEvent(SubscriptionId SubscriptionId, AccountId AccountId, TrialPeriod Trial) : DomainEvent;

/// <summary>Raised when a trial converts to a paying plan.</summary>
public sealed record SubscriptionConvertedEvent(SubscriptionId SubscriptionId, AccountId AccountId, PlanTier Plan) : DomainEvent;

/// <summary>Raised when a trial elapses (or an active plan is cancelled): account goes read-only.</summary>
public sealed record SubscriptionSuspendedEvent(SubscriptionId SubscriptionId, AccountId AccountId) : DomainEvent;

/// <summary>Raised when a suspended account pays and is instantly reactivated.</summary>
public sealed record SubscriptionReactivatedEvent(SubscriptionId SubscriptionId, AccountId AccountId, PlanTier Plan) : DomainEvent;

/// <summary>Raised when the retention window elapses and the account is closed/purged.</summary>
public sealed record SubscriptionExpiredEvent(SubscriptionId SubscriptionId, AccountId AccountId) : DomainEvent;
