using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>Raised when an organization starts its frictionless Pro trial.</summary>
public sealed record SubscriptionTrialStartedEvent(SubscriptionId SubscriptionId, OrganizationId OrganizationId, TrialPeriod Trial) : DomainEvent;

/// <summary>Raised when a trial converts to a paying plan.</summary>
public sealed record SubscriptionConvertedEvent(SubscriptionId SubscriptionId, OrganizationId OrganizationId, PlanTier Plan) : DomainEvent;

/// <summary>Raised when a trial elapses (or payment lapses): account goes read-only.</summary>
public sealed record SubscriptionSuspendedEvent(SubscriptionId SubscriptionId, OrganizationId OrganizationId) : DomainEvent;

/// <summary>Raised when a suspended account pays and is instantly reactivated.</summary>
public sealed record SubscriptionReactivatedEvent(SubscriptionId SubscriptionId, OrganizationId OrganizationId, PlanTier Plan) : DomainEvent;

/// <summary>Raised when the retention window elapses and the account is closed/purged.</summary>
public sealed record SubscriptionExpiredEvent(SubscriptionId SubscriptionId, OrganizationId OrganizationId) : DomainEvent;
