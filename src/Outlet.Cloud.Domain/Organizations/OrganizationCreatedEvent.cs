using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>Raised when an organization is created, with its first owner.</summary>
public sealed record OrganizationCreatedEvent(OrganizationId OrganizationId, MemberUserId OwnerId) : DomainEvent;
