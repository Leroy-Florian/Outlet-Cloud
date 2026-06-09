using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>Raised when a user is added to an organization.</summary>
public sealed record MemberAddedEvent(
    OrganizationId OrganizationId,
    MemberUserId UserId,
    OrganizationRole Role) : DomainEvent;
