using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Organizations;

/// <summary>
/// Organisation cliente (entreprise, équipe, sponsor). Une organisation peut
/// être prospect et payer pour plusieurs produits ; le rattachement des
/// prospects et paiements à une organisation est optionnel (sponsoring anonyme).
/// </summary>
public sealed class Organization : AggregateRoot<OrganizationId>
{
    private Organization(OrganizationId id, string name, string? website, DateTime createdAt)
        : base(id)
    {
        Name = name;
        Website = website;
        CreatedAt = createdAt;
    }

    public string Name { get; }

    public string? Website { get; }

    public DateTime CreatedAt { get; }

    public static Result<Organization> Create(string name, string? website, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Organization>(OrganizationErrors.NameRequired);
        }

        return Result.Success(new Organization(OrganizationId.New(), name.Trim(), website?.Trim(), createdAt));
    }
}
