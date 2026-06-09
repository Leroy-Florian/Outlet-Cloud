using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Cloud.Application.Organizations;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Application.Registry;
using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Web.Composition;
using Outlet.Identity.Application.AccessTokens;
using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// The session-gated organization management API (cookie auth). Authorization rules:
/// only Owner/Admin may manage members and roles; nobody may change or remove their
/// own membership; tokens are issued for the caller scoped to their role; removing a
/// member revokes that member's tokens for the organization.
/// </summary>
public static class OrganizationManagementEndpoints
{
    /// <summary>Default trial length (CLAUDE.md: 14 days, parameterizable).</summary>
    private const int TrialDays = 14;

    public static void MapOrganizationManagement(this WebApplication app)
    {
        var group = app.MapGroup("/organizations").RequireAuthorization();

        // Outlet Cloud (the whole management UI) is a paid offering: every endpoint here
        // requires a Pro account. The free tier is the public registry via the CLI.
        group.AddEndpointFilter(async (context, next) =>
        {
            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<OutletIdentityUser>>();
            var user = await users.GetUserAsync(context.HttpContext.User);
            if (user is null)
                return Results.Unauthorized();
            if (user.Plan != UserPlan.Pro)
                return Results.Json(new { error = "Outlet Cloud requires a Pro subscription." }, statusCode: StatusCodes.Status402PaymentRequired);

            return await next(context);
        });

        group.MapGet("/", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs) =>
        {
            var callerId = CallerId(principal, users);
            var mine = await orgs.ListForMemberAsync(MemberUserId.From(callerId));
            return Results.Ok(mine.Select(o => new
            {
                organizationId = o.Id.Value,
                slug = o.Slug.Value,
                name = o.Name.Value,
                role = RoleOf(o, callerId)?.ToString(),
            }));
        });

        group.MapPost("/", async (CreateOrganizationBody body, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, CreateOrganizationUseCase create, StartTrialUseCase startTrial) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null)
                return Results.Unauthorized();

            var result = await create.HandleAsync(new CreateOrganizationCommand(Guid.NewGuid(), body.Slug, body.Name, user.Id));

            // A new organization starts on a frictionless Pro trial; entitlements (publish,
            // quotas) are decided server-side from this subscription thereafter.
            if (result.IsSuccess)
                await startTrial.HandleAsync(new StartTrialCommand(result.Value, user.Email ?? string.Empty, TrialDays));

            return result.ToHttp(id => Results.Created($"/organizations/{id}", new { organizationId = id }));
        });

        group.MapGet("/{organizationId:guid}", async (Guid organizationId, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs) =>
        {
            var callerId = CallerId(principal, users);
            var org = await orgs.GetByIdAsync(OrganizationId.From(organizationId));
            if (org is null)
                return Results.NotFound();

            var callerRole = RoleOf(org, callerId);
            if (callerRole is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var members = new List<object>();
            foreach (var membership in org.Memberships)
            {
                var member = await users.FindByIdAsync(membership.Id.Value.ToString());
                members.Add(new
                {
                    userId = membership.Id.Value,
                    email = member?.Email,
                    displayName = member?.DisplayName,
                    role = membership.Role.ToString(),
                });
            }

            return Results.Ok(new
            {
                organizationId = org.Id.Value,
                slug = org.Slug.Value,
                name = org.Name.Value,
                role = callerRole.ToString(),
                members,
            });
        });

        group.MapPost("/{organizationId:guid}/members", async (Guid organizationId, AddMemberByEmailBody body, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, AddMemberUseCase add) =>
        {
            var (_, _, denied) = await AuthorizeManager(organizationId, principal, users, orgs);
            if (denied is not null)
                return denied;

            var target = await users.FindByEmailAsync(body.Email.Trim());
            if (target is null)
                return Results.BadRequest(new { error = $"No user with email '{body.Email}'." });

            var result = await add.HandleAsync(new AddMemberCommand(organizationId, target.Id, body.Role));
            return result.ToHttp();
        });

        group.MapPut("/{organizationId:guid}/members/{userId:guid}", async (Guid organizationId, Guid userId, ChangeRoleBody body, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, ChangeMemberRoleUseCase change) =>
        {
            var callerId = CallerId(principal, users);
            if (userId == callerId)
                return Results.BadRequest(new { error = "You cannot change your own role." });

            var (_, _, denied) = await AuthorizeManager(organizationId, principal, users, orgs);
            if (denied is not null)
                return denied;

            var result = await change.HandleAsync(new ChangeMemberRoleCommand(organizationId, userId, body.Role));
            return result.ToHttp();
        });

        group.MapDelete("/{organizationId:guid}/members/{userId:guid}", async (Guid organizationId, Guid userId, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, RemoveMemberUseCase remove, IPersonalAccessTokenRepository tokens, RevokePersonalAccessTokenUseCase revoke) =>
        {
            var callerId = CallerId(principal, users);
            if (userId == callerId)
                return Results.BadRequest(new { error = "You cannot remove yourself from the organization." });

            var (org, _, denied) = await AuthorizeManager(organizationId, principal, users, orgs);
            if (denied is not null)
                return denied;

            var result = await remove.HandleAsync(new RemoveMemberCommand(organizationId, userId));
            if (result.IsFailure)
                return result.ToHttp();

            // Revoke the removed member's tokens scoped to this organization.
            var prefix = $"org:{org!.Slug.Value}:";
            var owned = await tokens.ListForOwnerAsync(UserId.From(userId));
            foreach (var token in owned.Where(t => !t.IsRevoked && t.Scopes.Any(s => s.Value.StartsWith(prefix, StringComparison.Ordinal))))
                await revoke.HandleAsync(new RevokePersonalAccessTokenCommand(token.Id.Value));

            return Results.NoContent();
        });

        group.MapPost("/{organizationId:guid}/tokens", async (Guid organizationId, CreateTokenBody body, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, OrganizationTokenIssuer issuer) =>
        {
            var callerId = CallerId(principal, users);
            var result = await issuer.IssueAsync(organizationId, callerId, body.Name, body.ExpiresAtUtc);
            return result.ToHttp(token => Results.Created($"/organizations/{organizationId}/tokens/{token.TokenId}", token));
        });

        group.MapGet("/{organizationId:guid}/tokens", async (Guid organizationId, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, IPersonalAccessTokenRepository tokens) =>
        {
            var callerId = CallerId(principal, users);
            var org = await orgs.GetByIdAsync(OrganizationId.From(organizationId));
            if (org is null)
                return Results.NotFound();
            if (RoleOf(org, callerId) is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var prefix = $"org:{org.Slug.Value}:";
            var owned = await tokens.ListForOwnerAsync(UserId.From(callerId));
            return Results.Ok(owned
                .Where(t => t.Scopes.Any(s => s.Value.StartsWith(prefix, StringComparison.Ordinal)))
                .Select(t => new
                {
                    tokenId = t.Id.Value,
                    name = t.Name,
                    createdAtUtc = t.CreatedAtUtc,
                    expiresAtUtc = t.ExpiresAtUtc,
                    revoked = t.IsRevoked,
                }));
        });

        group.MapDelete("/{organizationId:guid}/tokens/{tokenId:guid}", async (Guid organizationId, Guid tokenId, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, IPersonalAccessTokenRepository tokens, RevokePersonalAccessTokenUseCase revoke) =>
        {
            var callerId = CallerId(principal, users);
            var org = await orgs.GetByIdAsync(OrganizationId.From(organizationId));
            if (org is null)
                return Results.NotFound();

            var callerRole = RoleOf(org, callerId);
            if (callerRole is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var token = await tokens.GetByIdAsync(PersonalAccessTokenId.From(tokenId));
            if (token is null)
                return Results.NotFound();

            var ownsToken = token.OwnerId == UserId.From(callerId);
            if (!ownsToken && !CanManage(callerRole.Value))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var result = await revoke.HandleAsync(new RevokePersonalAccessTokenCommand(tokenId));
            return result.ToHttp();
        });

        group.MapPost("/{organizationId:guid}/registry/items", async (Guid organizationId, PublishItemBody body, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, PublishItemUseCase publish) =>
        {
            var (_, _, denied) = await AuthorizeManager(organizationId, principal, users, orgs);
            if (denied is not null)
                return denied;

            IReadOnlyList<PublishedFileInput> files = [.. body.Files.Select(f => new PublishedFileInput(f.Path, f.Content))];
            var result = await publish.HandleAsync(new PublishItemCommand(organizationId, body.Name, body.Manifest.GetRawText(), files));
            return result.ToHttp(id => Results.Created($"/organizations/{organizationId}/registry/items/{body.Name}", new { publishedItemId = id }));
        });

        group.MapGet("/{organizationId:guid}/entitlements", async (Guid organizationId, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, GetEntitlementsUseCase entitlements) =>
        {
            var callerId = CallerId(principal, users);
            var org = await orgs.GetByIdAsync(OrganizationId.From(organizationId));
            if (org is null)
                return Results.NotFound();
            if (RoleOf(org, callerId) is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var result = await entitlements.HandleAsync(new GetEntitlementsQuery(organizationId));
            return result.ToHttp(view => Results.Ok(view));
        });

        group.MapGet("/{organizationId:guid}/registry/items", async (Guid organizationId, ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, IOrganizationRepository orgs, IPublishedItemRepository items) =>
        {
            var callerId = CallerId(principal, users);
            var org = await orgs.GetByIdAsync(OrganizationId.From(organizationId));
            if (org is null)
                return Results.NotFound();
            if (RoleOf(org, callerId) is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var published = await items.ListForOrganizationAsync(org.Id);
            return Results.Ok(published.Select(i => new { name = i.Name.Value, fileCount = i.Files.Count }));
        });
    }

    private static Guid CallerId(ClaimsPrincipal principal, UserManager<OutletIdentityUser> users) =>
        Guid.Parse(users.GetUserId(principal)!);

    private static OrganizationRole? RoleOf(Organization org, Guid userId) =>
        org.Memberships.FirstOrDefault(m => m.Id == MemberUserId.From(userId))?.Role;

    private static bool CanManage(OrganizationRole role) =>
        role is OrganizationRole.Owner or OrganizationRole.Admin;

    /// <summary>Loads the org and verifies the caller is an Owner/Admin; returns a 403/404 result when not.</summary>
    private static async Task<(Organization? Org, OrganizationRole? Role, IResult? Denied)> AuthorizeManager(
        Guid organizationId,
        ClaimsPrincipal principal,
        UserManager<OutletIdentityUser> users,
        IOrganizationRepository orgs)
    {
        var callerId = CallerId(principal, users);
        var org = await orgs.GetByIdAsync(OrganizationId.From(organizationId));
        if (org is null)
            return (null, null, Results.NotFound());

        var role = RoleOf(org, callerId);
        if (role is null || !CanManage(role.Value))
            return (org, role, Results.StatusCode(StatusCodes.Status403Forbidden));

        return (org, role, null);
    }
}

/// <summary>Create an organization (the caller becomes its first Owner).</summary>
public sealed record CreateOrganizationBody(string Slug, string Name);

/// <summary>Add a member to an organization by their email, with a role.</summary>
public sealed record AddMemberByEmailBody(string Email, OrganizationRole Role);

/// <summary>Change a member's role.</summary>
public sealed record ChangeRoleBody(OrganizationRole Role);

/// <summary>Issue a personal access token for the caller, scoped to their role in the organization.</summary>
public sealed record CreateTokenBody(string Name, DateTime? ExpiresAtUtc);

/// <summary>Publish (or replace) an item: its name, opaque manifest object, and files.</summary>
public sealed record PublishItemBody(string Name, JsonElement Manifest, IReadOnlyList<PublishedFileBody> Files);

/// <summary>A file of a published item.</summary>
public sealed record PublishedFileBody(string Path, string Content);
