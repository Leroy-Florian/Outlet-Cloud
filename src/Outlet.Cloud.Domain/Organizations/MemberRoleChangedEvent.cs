using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>Raised when an existing member's role changes.</summary>
public sealed record MemberRoleChangedEvent(
    OrganizationId OrganizationId,
    MemberUserId UserId,
    OrganizationRole Role) : DomainEvent;
