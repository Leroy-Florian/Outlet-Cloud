namespace Outlet.Crm.Domain.Organizations;

public readonly record struct OrganizationId(Guid Value)
{
    public static OrganizationId New() => new(Guid.NewGuid());
}
