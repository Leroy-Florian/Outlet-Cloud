using Outlet.Crm.Domain.Organizations;

namespace Outlet.Crm.Domain.UnitTests.Organizations;

public sealed class OrganizationTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_TrimNameAndWebsite_When_Created()
    {
        var result = Organization.Create("  Acme Corp ", " https://acme.example ", Now);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Acme Corp");
        result.Value!.Website.Should().Be("https://acme.example");
    }

    [Fact]
    public void Should_Fail_When_NameIsBlank()
    {
        var result = Organization.Create("  ", null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NameRequired);
    }
}
