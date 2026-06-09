namespace Outlet.Registry.Auth;

/// <summary>
/// SECONDARY PORT — looks a token up by its hash (never the plaintext). The authenticator
/// hashes the presented secret with the same rule used at minting, then asks you for the
/// stored token. Implement this over whatever persistence holds your tokens.
/// </summary>
public interface IPersonalAccessTokenStore
{
    Task<StoredAccessToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
