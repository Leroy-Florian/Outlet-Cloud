using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.AccessTokens;

/// <summary>Raised when a <see cref="PersonalAccessToken"/> is revoked and can no longer authenticate.</summary>
public sealed record PersonalAccessTokenRevokedEvent(PersonalAccessTokenId TokenId) : DomainEvent;
