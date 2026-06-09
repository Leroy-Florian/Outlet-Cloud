using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.Users;

/// <summary>Raised when a new <see cref="User"/> is registered. The Cloud context may
/// react (e.g. provision a personal organization).</summary>
public sealed record UserRegisteredEvent(UserId UserId, EmailAddress Email) : DomainEvent;
