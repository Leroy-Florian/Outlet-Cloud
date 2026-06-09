namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// A member's role within an organization, in increasing authority:
/// <see cref="Member"/> (consume the registry), <see cref="Admin"/> (manage
/// members and published items), <see cref="Owner"/> (full control; an org always
/// keeps at least one).
/// </summary>
public enum OrganizationRole
{
    Member = 0,
    Admin = 1,
    Owner = 2,
}
