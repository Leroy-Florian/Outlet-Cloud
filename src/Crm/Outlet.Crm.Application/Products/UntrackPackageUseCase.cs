using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record UntrackPackageCommand(Guid ProductId, PackageRegistry Registry, string PackageId);

/// <summary>Removes a NuGet/npm package from the set tracked by a product.</summary>
public sealed class UntrackPackageUseCase(IProductRepository products) : IUseCase<UntrackPackageCommand>
{
    public async Task<Result> HandleAsync(UntrackPackageCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        var packageId = PackageId.Create(command.PackageId);
        if (packageId.IsFailure)
        {
            return Result.Failure(packageId.Error!);
        }

        var untracked = product.UntrackPackage(command.Registry, packageId.Value!);
        if (untracked.IsFailure)
        {
            return untracked;
        }

        await products.UpdateAsync(product, cancellationToken);
        return Result.Success();
    }
}
