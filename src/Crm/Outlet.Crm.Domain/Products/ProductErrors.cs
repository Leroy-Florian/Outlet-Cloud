using Outlet.Crm.Domain.Analytics;

namespace Outlet.Crm.Domain.Products;

public static class ProductErrors
{
    public const string NameRequired =
        "Product.NameRequired: A product requires a non-empty name.";

    public static string NotFound(ProductId id) =>
        $"Product.NotFound: Product '{id.Value}' was not found.";

    public static string PackageAlreadyTracked(PackageRegistry registry, PackageId packageId) =>
        $"Product.PackageAlreadyTracked: Package '{packageId.Value}' ({registry}) is already tracked.";

    public static string PackageNotTracked(PackageRegistry registry, PackageId packageId) =>
        $"Product.PackageNotTracked: Package '{packageId.Value}' ({registry}) is not tracked by this product.";

    public static string RepositoryAlreadyTracked(RepositoryName repository) =>
        $"Product.RepositoryAlreadyTracked: Repository '{repository.FullName}' is already tracked.";
}
