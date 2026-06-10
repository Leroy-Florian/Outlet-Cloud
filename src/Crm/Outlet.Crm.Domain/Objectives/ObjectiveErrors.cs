namespace Outlet.Crm.Domain.Objectives;

public static class ObjectiveErrors
{
    public const string TargetNotPositive =
        "Objective.TargetNotPositive: An objective target must be strictly positive.";

    public const string InvalidMonth =
        "Objective.InvalidMonth: A month must be provided in 'yyyy-MM' format.";

    public static string NotFound(ObjectiveId id) =>
        $"Objective.NotFound: Objective '{id.Value}' was not found.";
}
