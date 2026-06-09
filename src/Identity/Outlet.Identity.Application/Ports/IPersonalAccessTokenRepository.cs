using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="PersonalAccessToken"/> aggregates.</summary>
public interface IPersonalAccessTokenRepository
{
    Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default);

    Task<PersonalAccessToken?> GetByIdAsync(PersonalAccessTokenId id, CancellationToken cancellationToken = default);

    /// <summary>Looks a token up by its hash — the authentication path when a bearer credential is presented.</summary>
    Task<PersonalAccessToken?> FindByHashAsync(TokenHash hash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalAccessToken>> ListForOwnerAsync(UserId ownerId, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalAccessToken token, CancellationToken cancellationToken = default);
}
