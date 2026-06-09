using Outlet.Identity.Application.AccessTokens;
using Outlet.Identity.Application.UnitTests.Fakes;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.UnitTests.AccessTokens;

public sealed class IssuePersonalAccessTokenUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRepository _users = new();
    private readonly FakePersonalAccessTokenRepository _tokens = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private IssuePersonalAccessTokenUseCase NewUseCase(string secret = "outlet_pat_testsecret") =>
        new(_users, _tokens, new FakeTokenSecretFactory(secret), new FixedClock(Now));

    private void SeedOwner() =>
        _users.Seed(User.Create(UserId.From(_ownerId), EmailAddress.From("owner@example.com"), "Owner").Value!);

    [Fact]
    public async Task Should_IssueAndPersist_When_OwnerExists()
    {
        SeedOwner();

        var result = await NewUseCase("outlet_pat_secret123").HandleAsync(
            new IssuePersonalAccessTokenCommand(_ownerId, "CI token", ["registry:read"]));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Secret.Should().Be("outlet_pat_secret123");
        _tokens.Count.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_OwnerDoesNotExist()
    {
        var result = await NewUseCase().HandleAsync(
            new IssuePersonalAccessTokenCommand(_ownerId, "CI token", ["registry:read"]));

        result.IsFailure.Should().BeTrue();
        _tokens.Count.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_ScopeIsInvalid()
    {
        SeedOwner();

        var result = await NewUseCase().HandleAsync(
            new IssuePersonalAccessTokenCommand(_ownerId, "CI token", ["bad scope"]));

        result.IsFailure.Should().BeTrue();
        _tokens.Count.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_NoScopesProvided()
    {
        SeedOwner();

        var result = await NewUseCase().HandleAsync(
            new IssuePersonalAccessTokenCommand(_ownerId, "CI token", []));

        result.IsFailure.Should().BeTrue();
        _tokens.Count.Should().Be(0);
    }
}
