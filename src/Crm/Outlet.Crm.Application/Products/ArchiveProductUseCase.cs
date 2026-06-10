using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record ArchiveProductCommand(Guid ProductId);

/// <summary>
/// Soft-archives a product (DELETE /api/products/{id} maps here): the product
/// keeps its snapshots/traffic history but disappears from the active dashboard.
/// </summary>
public sealed class ArchiveProductUseCase(IProductRepository products) : IUseCase<ArchiveProductCommand>
{
    public async Task<Result> HandleAsync(ArchiveProductCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        var archived = product.Archive();
        if (archived.IsFailure)
        {
            return archived;
        }

        await products.UpdateAsync(product, cancellationToken);
        return Result.Success();
    }
}
