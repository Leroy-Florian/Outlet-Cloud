using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// AGGREGATE ROOT — a tenant of Outlet Cloud that owns private registries and
/// groups its members. This is the authorization side of the product: who belongs
/// and with what <see cref="OrganizationRole"/>. It references users (from the
/// Identity context) by <see cref="MemberUserId"/> only.
///
/// Invariants enforced here:
/// - An organization always has at least one Owner (the last owner can neither be
///   removed nor demoted).
/// - A user appears at most once in the membership list.
/// </summary>
public sealed class Organization : AggregateRoot<OrganizationId>
{
    public OrganizationSlug Slug { get; }
    public OrganizationName Name { get; }

    private readonly List<Membership> _memberships;
    public IReadOnlyList<Membership> Memberships => _memberships;

    /// <summary>
    /// The account that hosts this organization's private registry (its first Owner).
    /// An organization always keeps at least one owner, so this is always present.
    /// </summary>
    public MemberUserId OwnerId => _memberships.First(m => m.Role == OrganizationRole.Owner).Id;

    private Organization(OrganizationId id, OrganizationSlug slug, OrganizationName name)
        : base(id)
    {
        Slug = slug;
        Name = name;
        _memberships = [];
    }

    public static Result<Organization> Create(
        OrganizationId id,
        OrganizationSlug slug,
        OrganizationName name,
        MemberUserId ownerId)
    {
        var organization = new Organization(id, slug, name);
        organization._memberships.Add(new Membership(ownerId, OrganizationRole.Owner));
        organization.RaiseDomainEvent(new OrganizationCreatedEvent(id, ownerId));

        return Result<Organization>.Success(organization);
    }

    /// <summary>
    /// Rehydrates an organization from TRUSTED persistence without raising creation
    /// events or re-running creation guards. Infrastructure-only entry point.
    /// </summary>
    public static Organization Restore(
        OrganizationId id,
        OrganizationSlug slug,
        OrganizationName name,
        IEnumerable<(MemberUserId UserId, OrganizationRole Role)> members)
    {
        var organization = new Organization(id, slug, name);
        foreach (var (userId, role) in members)
            organization._memberships.Add(new Membership(userId, role));

        return organization;
    }

    public Result AddMember(MemberUserId userId, OrganizationRole role)
    {
        if (Find(userId) is not null)
            return Result.Failure($"User '{userId}' is already a member of this organization.");

        _memberships.Add(new Membership(userId, role));
        RaiseDomainEvent(new MemberAddedEvent(Id, userId, role));

        return Result.Success();
    }

    public Result ChangeRole(MemberUserId userId, OrganizationRole role)
    {
        var member = Find(userId);
        if (member is null)
            return Result.Failure($"User '{userId}' is not a member of this organization.");

        if (member.Role == role)
            return Result.Success();

        if (WouldLeaveNoOwner(member, role))
            return Result.Failure("An organization must keep at least one owner.");

        member.ChangeRole(role);
        RaiseDomainEvent(new MemberRoleChangedEvent(Id, userId, role));

        return Result.Success();
    }

    public Result RemoveMember(MemberUserId userId)
    {
        var member = Find(userId);
        if (member is null)
            return Result.Failure($"User '{userId}' is not a member of this organization.");

        if (member.Role == OrganizationRole.Owner && OwnerCount == 1)
            return Result.Failure("An organization must keep at least one owner.");

        _memberships.Remove(member);
        RaiseDomainEvent(new MemberRemovedEvent(Id, userId));

        return Result.Success();
    }

    private Membership? Find(MemberUserId userId) =>
        _memberships.FirstOrDefault(m => m.Id == userId);

    private int OwnerCount => _memberships.Count(m => m.Role == OrganizationRole.Owner);

    private bool WouldLeaveNoOwner(Membership member, OrganizationRole newRole) =>
        member.Role == OrganizationRole.Owner && newRole != OrganizationRole.Owner && OwnerCount == 1;
}
