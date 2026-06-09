using Outlet.Identity.Application.AccessTokens;
using Outlet.Identity.Application.UnitTests.Fakes;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.UnitTests.AccessTokens;

public sealed class RevokePersonalAccessTokenUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePersonalAccessTokenRepository _tokens = new();

    private RevokePersonalAccessTokenUseCase NewUseCase() =>
        new(_tokens, new FixedClock(Now));

    private PersonalAccessToken SeedToken()
    {
        var token = PersonalAccessToken.Create(
            PersonalAccessTokenId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            "CI token",
            TokenHash.From("abcdef1234567890"),
            [TokenScope.From("registry:read")],
            Now).Value!;
        _tokens.Seed(token);
        return token;
    }

    [Fact]
    public async Task Should_Revoke_When_TokenExists()
    {
        var token = SeedToken();

        var result = await NewUseCase().HandleAsync(new RevokePersonalAccessTokenCommand(token.Id.Value));

        result.IsSuccess.Should().BeTrue();
        (await _tokens.GetByIdAsync(token.Id))!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_TokenNotFound()
    {
        var result = await NewUseCase().HandleAsync(new RevokePersonalAccessTokenCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_AlreadyRevoked()
    {
        var token = SeedToken();
        await NewUseCase().HandleAsync(new RevokePersonalAccessTokenCommand(token.Id.Value));

        var second = await NewUseCase().HandleAsync(new RevokePersonalAccessTokenCommand(token.Id.Value));

        second.IsFailure.Should().BeTrue();
    }
}
