using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;

namespace Outlet.Cloud.Domain.UnitTests.Registry;

public sealed class PublishedItemTests
{
    private static readonly PublishedItemId AnyId = PublishedItemId.From(Guid.NewGuid());
    private static readonly OrganizationId AnyOrg = OrganizationId.From(Guid.NewGuid());
    private static readonly RegistryItemName AnyName = RegistryItemName.From("email-smtp");

    [Fact]
    public void Should_Create_When_ManifestAndFilesArePresent()
    {
        var result = PublishedItem.Create(AnyId, AnyOrg, AnyName, "{\"name\":\"email-smtp\"}", [PublishedFile.From("a.cs", "// code")]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(AnyName);
        result.Value.Files.Should().ContainSingle();
    }

    [Fact]
    public void Should_Fail_When_ManifestIsBlank()
    {
        var result = PublishedItem.Create(AnyId, AnyOrg, AnyName, "  ", [PublishedFile.From("a.cs", "x")]);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("A published item must have a manifest.");
    }

    [Fact]
    public void Should_Fail_When_NoFilesAreShipped()
    {
        var result = PublishedItem.Create(AnyId, AnyOrg, AnyName, "{}", []);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("A published item must ship at least one file.");
    }

    [Fact]
    public void Should_RehydrateWithoutEvents_When_RestoredFromPersistence()
    {
        var item = PublishedItem.Restore(AnyId, AnyOrg, AnyName, "{}", [PublishedFile.From("a.cs", "x")]);

        item.OrganizationId.Should().Be(AnyOrg);
        item.ManifestJson.Should().Be("{}");
        item.Files.Should().ContainSingle();
        item.DomainEvents.Should().BeEmpty();
    }
}
