using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>Raised when an organization's registry visibility actually changes.</summary>
public sealed record RegistryVisibilityChangedEvent(
    OrganizationId OrganizationId,
    RegistryVisibility Visibility) : DomainEvent;
