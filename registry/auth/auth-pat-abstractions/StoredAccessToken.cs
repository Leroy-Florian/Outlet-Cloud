namespace Outlet.Registry.Auth;

/// <summary>
/// A persisted personal access token as the authenticator needs to see it: the owner,
/// the granted scopes, and the validity window. Project your own persistence model onto
/// this record inside your <see cref="IPersonalAccessTokenStore"/> implementation.
/// </summary>
public sealed record StoredAccessToken(
    Guid OwnerId,
    IReadOnlyList<string> Scopes,
    DateTimeOffset? ExpiresAt,
    bool IsRevoked)
{
    /// <summary>A token is usable when it is neither revoked nor past its expiry.</summary>
    public bool IsValidAt(DateTimeOffset now) =>
        !IsRevoked && (ExpiresAt is null || ExpiresAt > now);
}
