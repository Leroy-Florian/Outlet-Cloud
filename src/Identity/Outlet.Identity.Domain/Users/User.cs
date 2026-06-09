using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.Users;

/// <summary>
/// AGGREGATE ROOT — a person who can authenticate against Outlet Cloud.
///
/// This aggregate models the domain identity only: who the user is. Credentials
/// (password hash, lockout, 2FA, external logins) are delegated to ASP.NET Core
/// Identity in the Infrastructure layer — they are a storage/security detail, not
/// domain language. Other contexts reference a user by <see cref="UserId"/>.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    public EmailAddress Email { get; }
    public string DisplayName { get; }

    private User(UserId id, EmailAddress email, string displayName)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
    }

    public static Result<User> Create(UserId id, EmailAddress email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result<User>.Failure("User display name must not be empty.");

        var user = new User(id, email, displayName.Trim());
        user.RaiseDomainEvent(new UserRegisteredEvent(id, email));

        return Result<User>.Success(user);
    }

    /// <summary>Rehydrates a user from TRUSTED persistence without raising events. Infrastructure-only entry point.</summary>
    public static User Restore(UserId id, EmailAddress email, string displayName) => new(id, email, displayName);
}
