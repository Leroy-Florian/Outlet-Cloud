using Outlet.Cloud.Web.Authentication;
using Outlet.Cloud.Web.IntegrationTests.Fakes;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;
using Outlet.Identity.Infrastructure.Security;

namespace Outlet.Cloud.Web.IntegrationTests;

public sealed class PersonalAccessTokenAuthenticatorTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
    private const string Secret = "outlet_pat_abc123";

    private readonly FakePersonalAccessTokenRepository _tokens = new();

    private PersonalAccessTokenAuthenticator NewAuthenticator(DateTime now) =>
        new(_tokens, new FixedClock(now));

    private PersonalAccessToken SeedToken(DateTime? expiresAtUtc = null)
    {
        var token = PersonalAccessToken.Create(
            PersonalAccessTokenId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            "CI token",
            TokenHashing.ComputeHash(Secret),
            [TokenScope.From("registry:read")],
            Now,
            expiresAtUtc).Value!;
        _tokens.Seed(token);
        return token;
    }

    [Fact]
    public async Task Should_Authenticate_When_TokenIsValid()
    {
        var token = SeedToken();

        var result = await NewAuthenticator(Now).AuthenticateAsync($"Bearer {Secret}");

        result.Should().NotBeNull();
        result!.OwnerId.Should().Be(token.OwnerId.Value);
        result.Scopes.Should().Contain("registry:read");
    }

    [Fact]
    public async Task Should_Reject_When_TokenExpired()
    {
        SeedToken(Now.AddHours(1));

        var result = await NewAuthenticator(Now.AddHours(2)).AuthenticateAsync($"Bearer {Secret}");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Reject_When_TokenRevoked()
    {
        var token = SeedToken();
        token.Revoke(Now.AddMinutes(1));

        var result = await NewAuthenticator(Now.AddMinutes(2)).AuthenticateAsync($"Bearer {Secret}");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer outlet_pat_wrong")]
    [InlineData("Basic outlet_pat_abc123")]
    public async Task Should_Reject_When_HeaderIsMissingOrWrong(string? header)
    {
        SeedToken();

        var result = await NewAuthenticator(Now).AuthenticateAsync(header);

        result.Should().BeNull();
    }
}
