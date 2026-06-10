namespace Outlet.Crm.Domain.Prospects;

public static class ProspectErrors
{
    public const string NameRequired =
        "Prospect.NameRequired: A prospect requires a non-empty name.";

    public const string AlreadyClosed =
        "Prospect.AlreadyClosed: A won or lost prospect cannot change stage.";

    public const string InvalidTransition =
        "Prospect.InvalidTransition: Stages can only advance forward through the pipeline.";

    public const string LossReasonRequired =
        "Prospect.LossReasonRequired: Losing a prospect requires a non-empty reason.";

    public static string NotFound(ProspectId id) =>
        $"Prospect.NotFound: Prospect '{id.Value}' was not found.";
}
