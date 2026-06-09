using Outlet.Identity.Domain.Users;
using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.AccessTokens;

/// <summary>Raised when a <see cref="PersonalAccessToken"/> is issued for a user.</summary>
public sealed record PersonalAccessTokenIssuedEvent(
    PersonalAccessTokenId TokenId,
    UserId OwnerId) : DomainEvent;
