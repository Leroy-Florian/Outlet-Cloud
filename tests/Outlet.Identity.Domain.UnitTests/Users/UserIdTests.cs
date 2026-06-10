using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Domain.UnitTests.Users;

public sealed class UserIdTests
{
    [Fact]
    public void Should_Throw_When_ValueIsEmpty()
    {
        var act = () => UserId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("UserId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_IdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        UserId.From(guid).Should().Be(UserId.From(guid));
        UserId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_NotBeEqual_When_IdsDiffer()
    {
        UserId.From(Guid.NewGuid()).Should().NotBe(UserId.From(Guid.NewGuid()));
    }
}
