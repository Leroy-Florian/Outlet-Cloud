using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Domain.UnitTests.AccessTokens;

public sealed class PersonalAccessTokenIdTests
{
    [Fact]
    public void Should_Throw_When_ValueIsEmpty()
    {
        var act = () => PersonalAccessTokenId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("PersonalAccessTokenId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_IdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        PersonalAccessTokenId.From(guid).Should().Be(PersonalAccessTokenId.From(guid));
        PersonalAccessTokenId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_NotBeEqual_When_IdsDiffer()
    {
        PersonalAccessTokenId.From(Guid.NewGuid()).Should().NotBe(PersonalAccessTokenId.From(Guid.NewGuid()));
    }
}
