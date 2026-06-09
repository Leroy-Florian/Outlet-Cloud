namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// Maps an organization member's <see cref="OrganizationRole"/> to the registry
/// scope strings a token issued for them should carry. Scopes are PLAIN STRINGS
/// (<c>org:{slug}:registry:{action}</c>) so this Cloud policy stays decoupled from
/// the Identity context, which only sees opaque scopes. This is the bridge the
/// composition root uses before asking Identity to mint a token.
/// </summary>
public static class RegistryAccessPolicy
{
    public static IReadOnlyList<string> ScopesFor(OrganizationSlug organization, OrganizationRole role)
    {
        var prefix = $"org:{organization.Value}:registry:";

        return role switch
        {
            OrganizationRole.Owner => [prefix + "read", prefix + "write", prefix + "admin"],
            OrganizationRole.Admin => [prefix + "read", prefix + "write"],
            _ => [prefix + "read"],
        };
    }
}
