using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Products;

public sealed class ProductTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Product CreateProduct() => Product.Create("Accordent", "Packages npm", Now).Value!;

    [Fact]
    public void Should_Fail_When_NameIsBlank()
    {
        var result = Product.Create("  ", null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NameRequired);
    }

    [Fact]
    public void Should_TrackPackage_When_NotAlreadyTracked()
    {
        var product = CreateProduct();

        var result = product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);

        result.IsSuccess.Should().BeTrue();
        product.Packages.Should().ContainSingle().Which.Registry.Should().Be(PackageRegistry.Npm);
    }

    [Fact]
    public void Should_Fail_When_TrackingSamePackageTwice()
    {
        var product = CreateProduct();
        var packageId = PackageId.Create("@accordent/core").Value!;
        product.TrackPackage(PackageRegistry.Npm, packageId);

        var result = product.TrackPackage(PackageRegistry.Npm, packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.PackageAlreadyTracked:");
    }

    [Fact]
    public void Should_AllowSamePackageId_When_RegistriesDiffer()
    {
        var product = CreateProduct();
        var packageId = PackageId.Create("accordent").Value!;
        product.TrackPackage(PackageRegistry.Npm, packageId);

        var result = product.TrackPackage(PackageRegistry.NuGet, packageId);

        result.IsSuccess.Should().BeTrue();
        product.Packages.Should().HaveCount(2);
    }

    [Fact]
    public void Should_TrackRepository_When_NotAlreadyTracked()
    {
        var product = CreateProduct();

        var result = product.TrackRepository(RepositoryName.Create("Leroy-Florian/Accordent").Value!);

        result.IsSuccess.Should().BeTrue();
        product.Repositories.Should().ContainSingle();
    }

    [Fact]
    public void Should_Fail_When_TrackingSameRepositoryTwice()
    {
        var product = CreateProduct();
        var repository = RepositoryName.Create("Leroy-Florian/Accordent").Value!;
        product.TrackRepository(repository);

        var result = product.TrackRepository(repository);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.RepositoryAlreadyTracked:");
    }


    [Fact]
    public void Should_UpdateNameAndDescription_When_ProductIsActive()
    {
        var product = CreateProduct();

        var result = product.Update("  FluxPDF  ", "  PDF toolkit  ");

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("FluxPDF");
        product.Description.Should().Be("PDF toolkit");
    }

    [Fact]
    public void Should_ClearDescription_When_UpdatingWithNull()
    {
        var product = CreateProduct();

        product.Update("FluxPDF", null);

        product.Description.Should().BeNull();
    }

    [Fact]
    public void Should_FailUpdate_When_NameIsBlank()
    {
        var product = CreateProduct();

        var result = product.Update("  ", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NameRequired);
        product.Name.Should().Be("Accordent");
    }

    [Fact]
    public void Should_FailUpdate_When_ProductIsArchived()
    {
        var product = CreateProduct();
        product.Archive();

        var result = product.Update("FluxPDF", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.Archived(product.Id));
        product.Name.Should().Be("Accordent");
    }

    [Fact]
    public void Should_Archive_When_ProductIsActive()
    {
        var product = CreateProduct();

        var result = product.Archive();

        result.IsSuccess.Should().BeTrue();
        product.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void Should_FailArchive_When_AlreadyArchived()
    {
        var product = CreateProduct();
        product.Archive();

        var result = product.Archive();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.AlreadyArchived(product.Id));
    }

    [Fact]
    public void Should_FailTrackPackage_When_ProductIsArchived()
    {
        var product = CreateProduct();
        product.Archive();

        var result = product.TrackPackage(PackageRegistry.Npm, PackageId.Create("pkg").Value!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.Archived:");
        product.Packages.Should().BeEmpty();
    }

    [Fact]
    public void Should_FailTrackRepository_When_ProductIsArchived()
    {
        var product = CreateProduct();
        product.Archive();

        var result = product.TrackRepository(RepositoryName.Create("o/r").Value!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.Archived:");
        product.Repositories.Should().BeEmpty();
    }

    [Fact]
    public void Should_UntrackPackage_When_PackageIsTracked()
    {
        var product = CreateProduct();
        var packageId = PackageId.Create("@accordent/core").Value!;
        product.TrackPackage(PackageRegistry.Npm, packageId);

        var result = product.UntrackPackage(PackageRegistry.Npm, packageId);

        result.IsSuccess.Should().BeTrue();
        product.Packages.Should().BeEmpty();
    }

    [Fact]
    public void Should_FailUntrackPackage_When_RegistryDiffers()
    {
        var product = CreateProduct();
        var packageId = PackageId.Create("accordent").Value!;
        product.TrackPackage(PackageRegistry.Npm, packageId);

        var result = product.UntrackPackage(PackageRegistry.NuGet, packageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.PackageNotTracked:");
        product.Packages.Should().ContainSingle();
    }

    [Fact]
    public void Should_FailUntrackPackage_When_PackageIsNotTracked()
    {
        var product = CreateProduct();

        var result = product.UntrackPackage(PackageRegistry.Npm, PackageId.Create("pkg").Value!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.PackageNotTracked:");
    }

    [Fact]
    public void Should_UntrackRepository_When_RepositoryIsTracked()
    {
        var product = CreateProduct();
        var repository = RepositoryName.Create("Leroy-Florian/Accordent").Value!;
        product.TrackRepository(repository);

        var result = product.UntrackRepository(repository);

        result.IsSuccess.Should().BeTrue();
        product.Repositories.Should().BeEmpty();
    }

    [Fact]
    public void Should_FailUntrackRepository_When_RepositoryIsNotTracked()
    {
        var product = CreateProduct();

        var result = product.UntrackRepository(RepositoryName.Create("o/r").Value!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.RepositoryNotTracked:");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-slash")]
    [InlineData("/name")]
    [InlineData("owner/")]
    [InlineData("a/b/c")]
    public void Should_RejectRepositoryName_When_NotOwnerSlashName(string raw)
    {
        var result = RepositoryName.Create(raw);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("RepositoryName.Invalid:");
    }

    [Fact]
    public void Should_ParseOwnerAndName_When_RepositoryNameIsValid()
    {
        var result = RepositoryName.Create(" Leroy-Florian/Outlet-CLI ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Owner.Should().Be("Leroy-Florian");
        result.Value.Name.Should().Be("Outlet-CLI");
        result.Value.FullName.Should().Be("Leroy-Florian/Outlet-CLI");
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Should_RejectNegativeCounters_When_CreatingRepositorySnapshot(int openIssues, int stars, int forks)
    {
        var result = RepositorySnapshot.Create(
            ProductId.New(), RepositoryName.Create("a/b").Value!, openIssues, stars, forks, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("RepositorySnapshot.NegativeCount:");
    }

    [Fact]
    public void Should_AcceptZeroCounters_When_CreatingRepositorySnapshot()
    {
        var result = RepositorySnapshot.Create(
            ProductId.New(), RepositoryName.Create("a/b").Value!, 0, 0, 0, Now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_AcceptZeroDownloads_When_CreatingDownloadSnapshot()
    {
        var result = DownloadSnapshot.Create(
            ProductId.New(), PackageRegistry.Npm, PackageId.Create("pkg").Value!, 0, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalDownloads.Should().Be(0);
    }

    [Fact]
    public void Should_RejectNegativeDownloads_When_CreatingDownloadSnapshot()
    {
        var result = DownloadSnapshot.Create(
            ProductId.New(), PackageRegistry.Npm, PackageId.Create("pkg").Value!, -1, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("DownloadSnapshot.NegativeCount:");
    }
}
