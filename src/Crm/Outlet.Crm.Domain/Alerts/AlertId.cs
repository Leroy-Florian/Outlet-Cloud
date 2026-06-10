namespace Outlet.Crm.Domain.Alerts;

public readonly record struct AlertId(Guid Value)
{
    public static AlertId New() => new(Guid.NewGuid());
}
