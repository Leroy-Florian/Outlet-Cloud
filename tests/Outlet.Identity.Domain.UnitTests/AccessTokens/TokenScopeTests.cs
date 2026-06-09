using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Domain.UnitTests.AccessTokens;

public sealed class TokenScopeTests
{
    [Theory]
    [InlineData("registry:read")]
    [InlineData("org:acme:registry:write")]
    [InlineData("registry:*")]
    public void Should_Accept_When_ShapeIsValid(string value)
    {
        TokenScope.From(value).Value.Should().Be(value);
    }

    [Fact]
    public void Should_Normalize_When_ValueHasUppercaseAndWhitespace()
    {
        TokenScope.From("  Registry:Read  ").Value.Should().Be("registry:read");
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("bad/char")]
    public void Should_Throw_When_ShapeIsInvalid(string value)
    {
        var act = () => TokenScope.From(value);

        act.Should().Throw<ArgumentException>();
    }
}
