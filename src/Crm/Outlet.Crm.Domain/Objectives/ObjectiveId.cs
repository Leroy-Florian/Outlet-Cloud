namespace Outlet.Crm.Domain.Objectives;

public readonly record struct ObjectiveId(Guid Value)
{
    public static ObjectiveId New() => new(Guid.NewGuid());
}
