using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>
/// EF persistence model for an <see cref="Organization"/>. Kept separate from the
/// aggregate so the domain stays free of EF concerns (no parameterless ctor, no
/// public setters, no navigation pollution); the repository maps between the two.
/// </summary>
public sealed class OrganizationRecord
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<MembershipRecord> Members { get; set; } = [];
}

/// <summary>EF persistence model for a membership row.</summary>
public sealed class MembershipRecord
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrganizationRole Role { get; set; }
}
