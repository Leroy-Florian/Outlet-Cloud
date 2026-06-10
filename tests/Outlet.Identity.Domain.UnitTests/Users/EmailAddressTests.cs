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

    [Fact]
    public void Should_NotBeEqual_When_ValuesDiffer()
    {
        EmailAddress.From("bob@example.com").Should().NotBe(EmailAddress.From("alice@example.com"));
    }

    [Fact]
    public void Should_Throw_When_ValueIsBlank()
    {
        var act = () => EmailAddress.From("   ");

        act.Should().Throw<ArgumentException>().WithMessage("EmailAddress cannot be empty.*");
    }

    [Fact]
    public void Should_ThrowNotAValidEmail_When_AtSignIsTrailing()
    {
        var act = () => EmailAddress.From("user@");

        act.Should().Throw<ArgumentException>().WithMessage("*is not a valid email*");
    }

    [Fact]
    public void Should_ThrowInvalidDomain_When_DomainStartsWithADot()
    {
        var act = () => EmailAddress.From("user@.com");

        act.Should().Throw<ArgumentException>().WithMessage("*has an invalid domain*");
    }
}
