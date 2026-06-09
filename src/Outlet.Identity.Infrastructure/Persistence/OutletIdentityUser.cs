using Microsoft.AspNetCore.Identity;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>
/// ASP.NET Core Identity membership entity for the <c>User</c> aggregate: it owns the
/// credential concerns (password hash, lockout, 2FA, confirmation) while the domain
/// keeps only identity language. Keyed by the same GUID as the domain <c>UserId</c>.
///
/// The subscription/plan is NOT stored here — it is an Outlet.Cloud concern (the
/// <c>Subscription</c> aggregate, keyed by the same account GUID). Identity stays
/// decoupled from billing.
/// </summary>
public sealed class OutletIdentityUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
