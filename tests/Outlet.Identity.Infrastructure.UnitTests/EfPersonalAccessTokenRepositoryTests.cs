using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Identity.Infrastructure.UnitTests;

public sealed class EfPersonalAccessTokenRepositoryTests : IdentityDataContextFixture
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly UserId _owner = UserId.From(Guid.NewGuid());

    private static string UniqueHash() => Guid.NewGuid().ToString("N");

    private PersonalAccessToken NewToken(string hash) =>
        PersonalAccessToken.Create(
            PersonalAccessTokenId.From(Guid.NewGuid()),
            _owner,
            "CI token",
            TokenHash.From(hash),
            [TokenScope.From("registry:read"), TokenScope.From("registry:write")],
            Now,
            Now.AddDays(30)).Value!;

    [Fact]
    public async Task Should_RoundTrip_Token_WithScopes()
    {
        var token = NewToken(UniqueHash());
        await new EfPersonalAccessTokenRepository(NewContext()).AddAsync(token);

        var loaded = await new EfPersonalAccessTokenRepository(NewContext()).GetByIdAsync(token.Id);

        loaded.Should().NotBeNull();
        loaded!.OwnerId.Should().Be(_owner);
        loaded.Scopes.Select(s => s.Value).Should().BeEquivalentTo(["registry:read", "registry:write"]);
        loaded.IsValidAt(Now.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public async Task Should_FindByHash()
    {
        var hash = "abcdef1234567890";
        var token = NewToken(hash);
        await new EfPersonalAccessTokenRepository(NewContext()).AddAsync(token);

        var loaded = await new EfPersonalAccessTokenRepository(NewContext()).FindByHashAsync(TokenHash.From(hash));

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(token.Id);
    }

    [Fact]
    public async Task Should_PersistRevocation_OnUpdate()
    {
        var token = NewToken(UniqueHash());
        await new EfPersonalAccessTokenRepository(NewContext()).AddAsync(token);

        token.Revoke(Now.AddDays(1));
        await new EfPersonalAccessTokenRepository(NewContext()).UpdateAsync(token);

        var loaded = await new EfPersonalAccessTokenRepository(NewContext()).GetByIdAsync(token.Id);
        loaded!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ListForOwner()
    {
        await new EfPersonalAccessTokenRepository(NewContext()).AddAsync(NewToken(UniqueHash()));
        await new EfPersonalAccessTokenRepository(NewContext()).AddAsync(NewToken(UniqueHash()));

        var owned = await new EfPersonalAccessTokenRepository(NewContext()).ListForOwnerAsync(_owner);

        owned.Should().HaveCount(2);
    }
}
