using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>Raised when a user is removed from an organization.</summary>
public sealed record MemberRemovedEvent(OrganizationId OrganizationId, MemberUserId UserId) : DomainEvent;
