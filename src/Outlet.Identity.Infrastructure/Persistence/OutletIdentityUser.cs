using Microsoft.AspNetCore.Identity;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>Account subscription plan. Outlet Cloud (the web UI) requires <see cref="Pro"/>; the public registry stays free via the CLI.</summary>
public enum UserPlan
{
    Free = 0,
    Pro = 1,
}

/// <summary>
/// ASP.NET Core Identity membership entity for the <c>User</c> aggregate: it owns the
/// credential concerns (password hash, lockout, 2FA, confirmation) while the domain
/// keeps only identity language. Keyed by the same GUID as the domain <c>UserId</c>.
/// </summary>
public sealed class OutletIdentityUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The account's subscription plan; gates access to the Outlet Cloud web UI.</summary>
    public UserPlan Plan { get; set; } = UserPlan.Free;
}
