using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;

namespace Outlet.Cloud.Web.IntegrationTests.Fakes;

public sealed class FakePersonalAccessTokenRepository : IPersonalAccessTokenRepository
{
    private readonly List<PersonalAccessToken> _tokens = [];

    public void Seed(PersonalAccessToken token) => _tokens.Add(token);

    public Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default)
    {
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<PersonalAccessToken?> GetByIdAsync(PersonalAccessTokenId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.FirstOrDefault(t => t.Id == id));

    public Task<PersonalAccessToken?> FindByHashAsync(TokenHash hash, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.FirstOrDefault(t => t.Hash == hash));

    public Task<IReadOnlyList<PersonalAccessToken>> ListForOwnerAsync(UserId ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PersonalAccessToken>>([.. _tokens.Where(t => t.OwnerId == ownerId)]);

    public Task UpdateAsync(PersonalAccessToken token, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
