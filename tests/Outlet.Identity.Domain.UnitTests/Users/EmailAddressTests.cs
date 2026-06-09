using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Domain.UnitTests.Users;

public sealed class EmailAddressTests
{
    [Fact]
    public void Should_Normalize_When_ValueHasUppercaseAndWhitespace()
    {
        var email = EmailAddress.From("  Alice@Example.COM ");

        email.Value.Should().Be("alice@example.com");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@no-local.com")]
    [InlineData("no-domain@")]
    [InlineData("two@@ats.com")]
    [InlineData("missing@dotcom")]
    public void Should_Throw_When_ValueIsNotAnEmail(string value)
    {
        var act = () => EmailAddress.From(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_BeEqual_When_ValuesMatchAfterNormalization()
    {
        EmailAddress.From("Bob@Example.com").Should().Be(EmailAddress.From("bob@example.com"));
    }
}
