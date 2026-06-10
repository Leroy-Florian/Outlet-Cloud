namespace Outlet.Crm.Domain.Organizations;

public static class OrganizationErrors
{
    public const string NameRequired =
        "Organization.NameRequired: An organization requires a non-empty name.";

    public static string NotFound(OrganizationId id) =>
        $"Organization.NotFound: Organization '{id.Value}' was not found.";
}
