using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record TrackPackageCommand(Guid ProductId, PackageRegistry Registry, string PackageId);

/// <summary>Adds a NuGet/npm package to the set tracked by a product.</summary>
public sealed class TrackPackageUseCase(IProductRepository products) : IUseCase<TrackPackageCommand>
{
    public async Task<Result> HandleAsync(TrackPackageCommand command, CancellationToken cancellationToken = default)
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

        var tracked = product.TrackPackage(command.Registry, packageId.Value!);
        if (tracked.IsFailure)
        {
            return tracked;
        }

        await products.UpdateAsync(product, cancellationToken);
        return Result.Success();
    }
}
