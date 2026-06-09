using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.UnitTests.Fakes;

public sealed class FakePersonalAccessTokenRepository : IPersonalAccessTokenRepository
{
    private readonly Dictionary<Guid, PersonalAccessToken> _byId = [];

    public int Count => _byId.Count;

    public void Seed(PersonalAccessToken token) => _byId[token.Id.Value] = token;

    public Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default)
    {
        _byId[token.Id.Value] = token;
        return Task.CompletedTask;
    }

    public Task<PersonalAccessToken?> GetByIdAsync(PersonalAccessTokenId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<PersonalAccessToken?>(_byId.GetValueOrDefault(id.Value));

    public Task<PersonalAccessToken?> FindByHashAsync(TokenHash hash, CancellationToken cancellationToken = default) =>
        Task.FromResult<PersonalAccessToken?>(_byId.Values.FirstOrDefault(t => t.Hash == hash));

    public Task<IReadOnlyList<PersonalAccessToken>> ListForOwnerAsync(UserId ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PersonalAccessToken>>([.. _byId.Values.Where(t => t.OwnerId == ownerId)]);

    public Task UpdateAsync(PersonalAccessToken token, CancellationToken cancellationToken = default)
    {
        _byId[token.Id.Value] = token;
        return Task.CompletedTask;
    }
}
