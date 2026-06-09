using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Domain.UnitTests.Organizations;

public sealed class OrganizationSlugTests
{
    [Fact]
    public void Should_Normalize_When_ValueHasUppercaseAndWhitespace()
    {
        OrganizationSlug.From("  Acme-Corp  ").Value.Should().Be("acme-corp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("UPPER_score")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public void Should_Throw_When_ShapeIsInvalid(string value)
    {
        var act = () => OrganizationSlug.From(value);

        act.Should().Throw<ArgumentException>();
    }
}
