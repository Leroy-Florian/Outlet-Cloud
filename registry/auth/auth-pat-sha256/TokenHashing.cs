using System.Security.Cryptography;
using System.Text;

namespace Outlet.Registry.Auth;

/// <summary>
/// The single hashing rule for personal access tokens: SHA-256 of the UTF-8 secret,
/// lowercase hex. The exact same rule must be used when minting a token and when
/// validating a presented credential — otherwise the two sides drift apart and every
/// token fails. Keep this file as the one place that rule lives.
/// </summary>
public static class TokenHashing
{
    public static string ComputeHash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
}
