using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Domain.UnitTests.AccessTokens;

public sealed class PersonalAccessTokenTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    private static readonly PersonalAccessTokenId AnyId = PersonalAccessTokenId.From(Guid.NewGuid());
    private static readonly UserId AnyOwner = UserId.From(Guid.NewGuid());
    private static readonly TokenHash AnyHash = TokenHash.From("abcdef12");

    private static readonly IReadOnlyCollection<TokenScope> AnyScopes =
        [TokenScope.From("registry:read")];

    [Fact]
    public void Should_Succeed_When_AllInvariantsHold()
    {
        var result = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, AnyScopes, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OwnerId.Should().Be(AnyOwner);
        result.Value.IsRevoked.Should().BeFalse();
        result.Value.DomainEvents.Should().ContainSingle(e => e is PersonalAccessTokenIssuedEvent);
    }

    [Fact]
    public void Should_Fail_When_NoScopeIsGranted()
    {
        var result = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, [], Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ExpiryIsNotAfterCreation()
    {
        var result = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, AnyScopes, Now, Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_DeduplicateScopes_When_SameScopeProvidedTwice()
    {
        var result = PersonalAccessToken.Create(
            AnyId, AnyOwner, "CI token", AnyHash,
            [TokenScope.From("registry:read"), TokenScope.From("registry:read")],
            Now);

        result.Value!.Scopes.Should().ContainSingle();
    }

    [Fact]
    public void Should_BeInvalid_When_ExpiredAtCheckTime()
    {
        var token = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, AnyScopes, Now, Now.AddHours(1)).Value!;

        token.IsValidAt(Now.AddMinutes(30)).Should().BeTrue();
        token.IsValidAt(Now.AddHours(2)).Should().BeFalse();
    }

    [Fact]
    public void Should_BeInvalid_When_CheckedExactlyAtExpiry()
    {
        var token = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, AnyScopes, Now, Now.AddHours(1)).Value!;

        token.IsValidAt(Now.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void Should_RaiseRevokedEvent_When_RevokedFirstTime()
    {
        var token = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, AnyScopes, Now).Value!;

        var result = token.Revoke(Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        token.IsValidAt(Now.AddHours(2)).Should().BeFalse();
        token.DomainEvents.Should().ContainSingle(e => e is PersonalAccessTokenRevokedEvent);
    }

    [Fact]
    public void Should_Fail_When_RevokedTwice()
    {
        var token = PersonalAccessToken.Create(AnyId, AnyOwner, "CI token", AnyHash, AnyScopes, Now).Value!;
        token.Revoke(Now.AddHours(1));

        var second = token.Revoke(Now.AddHours(2));

        second.IsFailure.Should().BeTrue();
    }
}
