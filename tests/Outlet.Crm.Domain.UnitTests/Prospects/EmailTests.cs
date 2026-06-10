using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests.Prospects;

public sealed class EmailTests
{
    [Theory]
    [InlineData("ada@example.com")]
    [InlineData("  ADA@Example.COM  ")]
    public void Should_NormalizeValue_When_EmailIsValid(string raw)
    {
        var result = Email.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("ada@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("ada@")]
    [InlineData("ada lovelace@example.com")]
    public void Should_Fail_When_EmailIsInvalid(string raw)
    {
        var result = Email.Create(raw);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Email.Invalid:");
    }
}
