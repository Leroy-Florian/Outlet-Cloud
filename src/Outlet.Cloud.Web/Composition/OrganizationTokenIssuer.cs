using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Identity.Application.AccessTokens;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Web.Composition;

/// <summary>
/// COMPOSITION ROOT orchestration that crosses both bounded contexts (which is why
/// it lives here, not inside either): it reads the caller's membership/role in the
/// Cloud context, derives the registry scopes via <see cref="RegistryAccessPolicy"/>,
/// then asks the Identity context to mint a scoped personal access token.
/// </summary>
public sealed class OrganizationTokenIssuer(
    IOrganizationRepository organizations,
    IssuePersonalAccessTokenUseCase issueToken)
{
    public async Task<Result<IssuedPersonalAccessToken>> IssueAsync(
        Guid organizationId,
        Guid userId,
        string name,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var orgIdResult = Guard.TryBuild(() => OrganizationId.From(organizationId), "Organization id is invalid.");
        if (orgIdResult.IsFailure)
            return Result<IssuedPersonalAccessToken>.Failure(orgIdResult.Error!);

        var memberResult = Guard.TryBuild(() => MemberUserId.From(userId), "User id is invalid.");
        if (memberResult.IsFailure)
            return Result<IssuedPersonalAccessToken>.Failure(memberResult.Error!);

        var organization = await organizations.GetByIdAsync(orgIdResult.Value!, cancellationToken);
        if (organization is null)
            return Result<IssuedPersonalAccessToken>.Failure($"Organization '{orgIdResult.Value}' was not found.");

        var membership = organization.Memberships.FirstOrDefault(m => m.Id == memberResult.Value!);
        if (membership is null)
            return Result<IssuedPersonalAccessToken>.Failure("User is not a member of this organization.");

        var scopes = RegistryAccessPolicy.ScopesFor(organization.Slug, membership.Role);

        return await issueToken.HandleAsync(
            new IssuePersonalAccessTokenCommand(userId, name, scopes, expiresAtUtc),
            cancellationToken);
    }
}
