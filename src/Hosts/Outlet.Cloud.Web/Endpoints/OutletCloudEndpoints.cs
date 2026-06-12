using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Cloud.Web.Authentication;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// The machine-facing registry: authenticated by a personal access token (bearer),
/// authorized by the token's <c>org:{slug}:registry:read</c> scope. Serves the index
/// and item files in the exact layout the CLI's HttpRegistrySource consumes
/// (<c>{base}/registry.json</c> + <c>{base}/{item}/{file}</c>). Human/management
/// endpoints live in <see cref="OrganizationManagementEndpoints"/> (cookie session).
/// </summary>
public static class OutletCloudEndpoints
{
    public static void MapOutletCloud(this WebApplication app)
    {
        app.MapGet("/organizations/{slug}/registry.json", async (
            string slug,
            HttpRequest http,
            PersonalAccessTokenAuthenticator authenticator,
            IOrganizationRepository orgs,
            IPublishedItemRepository items,
            SubscriptionEntitlementResolver entitlements,
            CancellationToken ct) =>
        {
            var (org, scopeDenied) = await AuthorizeRead(slug, http, authenticator, orgs, entitlements, ct);
            if (scopeDenied is not null)
                return scopeDenied;

            var published = await items.ListForOrganizationAsync(org!.Id, ct);
            var json = "{\"items\":[" + string.Join(",", published.Select(i => i.ManifestJson)) + "]}";
            return Results.Content(json, "application/json");
        });

        app.MapGet("/organizations/{slug}/{itemName}/{**filePath}", async (
            string slug,
            string itemName,
            string filePath,
            HttpRequest http,
            PersonalAccessTokenAuthenticator authenticator,
            IOrganizationRepository orgs,
            IPublishedItemRepository items,
            SubscriptionEntitlementResolver entitlements,
            CancellationToken ct) =>
        {
            var (org, scopeDenied) = await AuthorizeRead(slug, http, authenticator, orgs, entitlements, ct);
            if (scopeDenied is not null)
                return scopeDenied;

            var nameResult = Guard.TryBuild(() => RegistryItemName.From(itemName), "invalid item name");
            if (nameResult.IsFailure)
                return Results.NotFound();

            var item = await items.GetAsync(org!.Id, nameResult.Value!, ct);
            var file = item?.Files.FirstOrDefault(f => f.Path == filePath);
            return file is null ? Results.NotFound() : Results.Text(file.Content);
        });
    }

    /// <summary>
    /// Resolves the org and authorizes the read. A registry whose organization opted into
    /// <see cref="RegistryVisibility.Public"/> is readable anonymously; anything else (private
    /// registry, unknown slug) demands a bearer token + read scope first, so an anonymous probe
    /// cannot distinguish a missing org from a private one. Returns a denial result otherwise.
    /// </summary>
    private static async Task<(Organization? Org, IResult? Denied)> AuthorizeRead(
        string slug,
        HttpRequest http,
        PersonalAccessTokenAuthenticator authenticator,
        IOrganizationRepository orgs,
        SubscriptionEntitlementResolver entitlements,
        CancellationToken ct)
    {
        var slugResult = Guard.TryBuild(() => OrganizationSlug.From(slug), "invalid slug");
        var org = slugResult.IsSuccess ? await orgs.GetBySlugAsync(slugResult.Value!, ct) : null;

        if (org is null || org.RegistryVisibility != RegistryVisibility.Public)
        {
            var token = await authenticator.AuthenticateAsync(http.Headers.Authorization, ct);
            if (token is null)
                return (null, Results.Unauthorized());

            if (!token.Scopes.Contains($"org:{slug}:registry:read"))
                return (null, Results.StatusCode(StatusCodes.Status403Forbidden));

            if (org is null)
                return (null, Results.NotFound());
        }

        // The registry is hosted under the owner's plan. Trial/Active/Suspended may still be
        // read (consult/export); only an expired (purged) account loses read access.
        var hostEntitlements = await entitlements.ResolveAsync(AccountId.From(org.OwnerId.Value), ct);
        if (!hostEntitlements.CanReadPrivateRegistry)
            return (null, Results.Json(
                new { error = "This private registry is unavailable: the hosting account's Outlet Cloud subscription has expired." },
                statusCode: StatusCodes.Status402PaymentRequired));

        return (org, null);
    }
}
