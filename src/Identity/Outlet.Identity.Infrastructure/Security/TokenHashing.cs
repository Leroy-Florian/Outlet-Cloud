using System.Security.Cryptography;
using System.Text;
using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Infrastructure.Security;

/// <summary>
/// The single hashing rule for personal access tokens: SHA-256 of the UTF-8 secret,
/// lowercase hex. Shared by the minting side (<see cref="Sha256TokenSecretFactory"/>)
/// and the validation side (the bearer authenticator) so they can never drift apart.
/// </summary>
public static class TokenHashing
{
    public static TokenHash ComputeHash(string secret) =>
        TokenHash.From(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret))));
}
