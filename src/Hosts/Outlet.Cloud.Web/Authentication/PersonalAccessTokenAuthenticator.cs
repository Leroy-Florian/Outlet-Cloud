using Outlet.Identity.Application.Ports;
using Outlet.Identity.Infrastructure.Security;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Web.Authentication;

/// <summary>The principal behind a valid bearer token: who owns it and what it may do.</summary>
public sealed record AuthenticatedToken(Guid OwnerId, IReadOnlyList<string> Scopes);

/// <summary>
/// Validates an incoming <c>Authorization: Bearer outlet_pat_…</c> credential by
/// hashing it (same rule as minting) and looking it up; rejects unknown, expired
/// and revoked tokens. Returns the owner and granted scopes, or null.
/// </summary>
public sealed class PersonalAccessTokenAuthenticator(
    IPersonalAccessTokenRepository tokens,
    ICurrentDateTimeProvider clock)
{
    private const string BearerPrefix = "Bearer ";

    public async Task<AuthenticatedToken?> AuthenticateAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var secret = authorizationHeader[BearerPrefix.Length..].Trim();
        if (secret.Length == 0)
            return null;

        var token = await tokens.FindByHashAsync(TokenHashing.ComputeHash(secret), cancellationToken);
        if (token is null || !token.IsValidAt(clock.UtcNow))
            return null;

        return new AuthenticatedToken(token.OwnerId.Value, [.. token.Scopes.Select(s => s.Value)]);
    }
}
