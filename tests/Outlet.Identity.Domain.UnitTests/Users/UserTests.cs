using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Domain.UnitTests.Users;

public sealed class UserTests
{
    private static readonly UserId AnyId = UserId.From(Guid.NewGuid());
    private static readonly EmailAddress AnyEmail = EmailAddress.From("alice@example.com");

    [Fact]
    public void Should_Succeed_When_DisplayNameIsProvided()
    {
        var result = User.Create(AnyId, AnyEmail, "  Alice  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Alice");
        result.Value.Email.Should().Be(AnyEmail);
    }

    [Fact]
    public void Should_RaiseRegisteredEvent_When_Created()
    {
        var result = User.Create(AnyId, AnyEmail, "Alice");

        result.Value!.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_DisplayNameIsBlank(string displayName)
    {
        var result = User.Create(AnyId, AnyEmail, displayName);

        result.IsFailure.Should().BeTrue();
    }
}
