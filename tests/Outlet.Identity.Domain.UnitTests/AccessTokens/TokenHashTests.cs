using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Domain.UnitTests.AccessTokens;

public sealed class TokenHashTests
{
    [Fact]
    public void Should_Normalize_When_DigestIsUppercase()
    {
        TokenHash.From("ABCDEF12").Value.Should().Be("abcdef12");
    }

    [Theory]
    [InlineData("")]
    [InlineData("xyz")]
    [InlineData("abc")]
    public void Should_Throw_When_DigestIsNotEvenLengthHex(string value)
    {
        var act = () => TokenHash.From(value);

        act.Should().Throw<ArgumentException>();
    }
}
