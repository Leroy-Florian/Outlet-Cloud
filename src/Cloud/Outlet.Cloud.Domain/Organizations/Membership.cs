using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// A user's membership in an <see cref="Organization"/> — a child entity of the
/// organization aggregate, identified by the (referenced) user's id. Created and
/// mutated only through the <see cref="Organization"/> root, never directly.
/// </summary>
public sealed class Membership : Entity<MemberUserId>
{
    public OrganizationRole Role { get; private set; }

    internal Membership(MemberUserId userId, OrganizationRole role)
        : base(userId)
    {
        Role = role;
    }

    internal void ChangeRole(OrganizationRole role) => Role = role;
}
