namespace Outlet.Crm.Domain.Prospects;

public readonly record struct ProspectId(Guid Value)
{
    public static ProspectId New() => new(Guid.NewGuid());
}
