using Outlet.Crm.Application.Products;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Products;

public sealed class ProductManagementUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();

    private Product Seed()
    {
        var product = Product.Create("Accordent", "Packages npm", Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    [Fact]
    public async Task Should_UpdateProduct_When_CommandIsValid()
    {
        var product = Seed();
        var useCase = new UpdateProductUseCase(_products);

        var result = await useCase.HandleAsync(
            new UpdateProductCommand(product.Id.Value, "FluxPDF", "PDF toolkit"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("FluxPDF");
        product.Description.Should().Be("PDF toolkit");
        _products.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailUpdate_When_ProductIsUnknown()
    {
        var useCase = new UpdateProductUseCase(_products);

        var result = await useCase.HandleAsync(
            new UpdateProductCommand(Guid.NewGuid(), "FluxPDF", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_FailUpdate_When_NameIsBlank()
    {
        var product = Seed();
        var useCase = new UpdateProductUseCase(_products);

        var result = await useCase.HandleAsync(
            new UpdateProductCommand(product.Id.Value, " ", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NameRequired:");
        _products.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ArchiveProduct_When_ProductIsActive()
    {
        var product = Seed();
        var useCase = new ArchiveProductUseCase(_products);

        var result = await useCase.HandleAsync(new ArchiveProductCommand(product.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.IsArchived.Should().BeTrue();
        _products.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailArchive_When_ProductIsUnknown()
    {
        var useCase = new ArchiveProductUseCase(_products);

        var result = await useCase.HandleAsync(new ArchiveProductCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_FailArchive_When_ProductIsAlreadyArchived()
    {
        var product = Seed();
        product.Archive();
        var useCase = new ArchiveProductUseCase(_products);

        var result = await useCase.HandleAsync(new ArchiveProductCommand(product.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.AlreadyArchived:");
        _products.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UntrackPackage_When_PackageIsTracked()
    {
        var product = Seed();
        product.TrackPackage(PackageRegistry.Npm, PackageId.Create("@accordent/core").Value!);
        var useCase = new UntrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackPackageCommand(product.Id.Value, PackageRegistry.Npm, "@accordent/core"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Packages.Should().BeEmpty();
        _products.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailUntrackPackage_When_ProductIsUnknown()
    {
        var useCase = new UntrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackPackageCommand(Guid.NewGuid(), PackageRegistry.Npm, "pkg"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_FailUntrackPackage_When_PackageIdIsBlank()
    {
        var product = Seed();
        var useCase = new UntrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackPackageCommand(product.Id.Value, PackageRegistry.Npm, "  "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("PackageId.Empty:");
    }

    [Fact]
    public async Task Should_FailUntrackPackage_When_PackageIsNotTracked()
    {
        var product = Seed();
        var useCase = new UntrackPackageUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackPackageCommand(product.Id.Value, PackageRegistry.Npm, "pkg"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.PackageNotTracked:");
        _products.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UntrackRepository_When_RepositoryIsTracked()
    {
        var product = Seed();
        product.TrackRepository(RepositoryName.Create("Leroy-Florian/Accordent").Value!);
        var useCase = new UntrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackRepositoryCommand(product.Id.Value, "Leroy-Florian/Accordent"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Repositories.Should().BeEmpty();
        _products.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailUntrackRepository_When_ProductIsUnknown()
    {
        var useCase = new UntrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackRepositoryCommand(Guid.NewGuid(), "o/r"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_FailUntrackRepository_When_NameIsInvalid()
    {
        var product = Seed();
        var useCase = new UntrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackRepositoryCommand(product.Id.Value, "not-a-repo"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("RepositoryName.Invalid:");
    }

    [Fact]
    public async Task Should_FailUntrackRepository_When_RepositoryIsNotTracked()
    {
        var product = Seed();
        var useCase = new UntrackRepositoryUseCase(_products);

        var result = await useCase.HandleAsync(
            new UntrackRepositoryCommand(product.Id.Value, "o/r"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.RepositoryNotTracked:");
        _products.UpdateCount.Should().Be(0);
    }
}
