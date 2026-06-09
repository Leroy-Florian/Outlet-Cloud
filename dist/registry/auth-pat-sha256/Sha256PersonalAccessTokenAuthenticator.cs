using Microsoft.Extensions.Options;

namespace Outlet.Registry.Auth;

/// <summary>
/// Validates an incoming <c>Authorization: Bearer …</c> credential by hashing it (the
/// same SHA-256 rule used at minting) and looking it up through
/// <see cref="IPersonalAccessTokenStore"/>; rejects missing, malformed, unknown, expired
/// and revoked tokens. Returns the owner and granted scopes, or <c>null</c>. The wall
/// clock comes from <see cref="TimeProvider"/> so expiry checks stay testable.
/// </summary>
public sealed class Sha256PersonalAccessTokenAuthenticator(
    IPersonalAccessTokenStore store,
    TimeProvider clock,
    IOptions<PersonalAccessTokenAuthenticationOptions> options) : IPersonalAccessTokenAuthenticator
{
    private readonly string _bearerPrefix = options.Value.BearerPrefix;

    public async Task<AuthenticatedToken?> AuthenticateAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(_bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var secret = authorizationHeader[_bearerPrefix.Length..].Trim();
        if (secret.Length == 0)
            return null;

        var token = await store.FindByHashAsync(TokenHashing.ComputeHash(secret), cancellationToken);
        if (token is null || !token.IsValidAt(clock.GetUtcNow()))
            return null;

        return new AuthenticatedToken(token.OwnerId, token.Scopes);
    }
}
