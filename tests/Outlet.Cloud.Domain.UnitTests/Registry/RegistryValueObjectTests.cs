using Outlet.Cloud.Domain.Registry;

namespace Outlet.Cloud.Domain.UnitTests.Registry;

public sealed class RegistryValueObjectTests
{
    [Fact]
    public void Should_Throw_When_PublishedItemIdIsEmpty()
    {
        var act = () => PublishedItemId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("PublishedItemId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_PublishedItemIdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        PublishedItemId.From(guid).Should().Be(PublishedItemId.From(guid));
        PublishedItemId.From(guid).GetHashCode().Should().Be(PublishedItemId.From(guid).GetHashCode());
        PublishedItemId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_NormalizeToLowercase_When_RegistryItemNameIsCreated()
    {
        var name = RegistryItemName.From("  Email-SMTP ");

        name.Value.Should().Be("email-smtp");
        name.ToString().Should().Be("email-smtp");
        name.Should().Be(RegistryItemName.From("email-smtp"));
    }

    [Fact]
    public void Should_Throw_When_RegistryItemNameIsBlank()
    {
        var act = () => RegistryItemName.From("   ");

        act.Should().Throw<ArgumentException>().WithMessage("RegistryItemName cannot be empty.*");
    }

    [Fact]
    public void Should_Throw_When_RegistryItemNameContainsInvalidCharacters()
    {
        var act = () => RegistryItemName.From("email_smtp");

        act.Should().Throw<ArgumentException>().WithMessage("*must be lowercase kebab-case*");
    }

    [Fact]
    public void Should_Throw_When_RegistryItemNameStartsOrEndsWithDash()
    {
        var leading = () => RegistryItemName.From("-email");
        var trailing = () => RegistryItemName.From("email-");

        leading.Should().Throw<ArgumentException>().WithMessage("*must not start or end with '-'*");
        trailing.Should().Throw<ArgumentException>().WithMessage("*must not start or end with '-'*");
    }

    [Fact]
    public void Should_TrimPathAndDefaultContent_When_PublishedFileIsCreated()
    {
        var file = PublishedFile.From("  src/A.cs ", null!);

        file.Path.Should().Be("src/A.cs");
        file.Content.Should().BeEmpty();
        file.ToString().Should().Be("src/A.cs");
    }

    [Fact]
    public void Should_Throw_When_PublishedFilePathIsBlank()
    {
        var act = () => PublishedFile.From(" ", "content");

        act.Should().Throw<ArgumentException>().WithMessage("PublishedFile path cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_PublishedFilesShareTheSamePathAndContent()
    {
        PublishedFile.From("a.cs", "x").Should().Be(PublishedFile.From("a.cs", "x"));
        PublishedFile.From("a.cs", "x").Should().NotBe(PublishedFile.From("a.cs", "y"));
    }
}
