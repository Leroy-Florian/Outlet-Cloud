using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Domain.UnitTests.Organizations;

public sealed class RegistryAccessPolicyTests
{
    private static readonly OrganizationSlug Acme = OrganizationSlug.From("acme");

    [Fact]
    public void Should_GrantReadOnly_When_Member()
    {
        RegistryAccessPolicy.ScopesFor(Acme, OrganizationRole.Member)
            .Should().BeEquivalentTo(["org:acme:registry:read"]);
    }

    [Fact]
    public void Should_GrantReadWrite_When_Admin()
    {
        RegistryAccessPolicy.ScopesFor(Acme, OrganizationRole.Admin)
            .Should().BeEquivalentTo(["org:acme:registry:read", "org:acme:registry:write"]);
    }

    [Fact]
    public void Should_GrantAdmin_When_Owner()
    {
        RegistryAccessPolicy.ScopesFor(Acme, OrganizationRole.Owner)
            .Should().Contain("org:acme:registry:admin");
    }
}
