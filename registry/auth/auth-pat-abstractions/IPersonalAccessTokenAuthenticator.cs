namespace Outlet.Registry.Auth;

/// <summary>
/// PRIMARY PORT — validates an incoming <c>Authorization: Bearer …</c> credential and
/// resolves the owning user and granted scopes. Returns <c>null</c> when the credential
/// is missing, malformed, unknown, expired or revoked. Generic and provider-agnostic:
/// the hashing rule and persistence live behind the adapter, never in this port.
/// </summary>
public interface IPersonalAccessTokenAuthenticator
{
    Task<AuthenticatedToken?> AuthenticateAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
}
