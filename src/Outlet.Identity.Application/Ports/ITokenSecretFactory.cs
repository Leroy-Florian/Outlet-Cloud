using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Application.Ports;

/// <summary>
/// SECONDARY PORT — mints a fresh token: a cryptographically random secret, its
/// public id and the <see cref="TokenHash"/> persisted with the aggregate. The
/// RNG and hashing live in Infrastructure; the secret is returned once and never
/// stored.
/// </summary>
public interface ITokenSecretFactory
{
    GeneratedTokenSecret Create();
}

/// <summary>The output of minting a token: the public id, the one-time plaintext secret, and the digest to persist.</summary>
public sealed record GeneratedTokenSecret(PersonalAccessTokenId Id, string Secret, TokenHash Hash);
