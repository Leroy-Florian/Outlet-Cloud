using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record UpdateProductCommand(Guid ProductId, string Name, string? Description);

/// <summary>Renames a product and/or changes its description.</summary>
public sealed class UpdateProductUseCase(IProductRepository products) : IUseCase<UpdateProductCommand>
{
    public async Task<Result> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        var updated = product.Update(command.Name, command.Description);
        if (updated.IsFailure)
        {
            return updated;
        }

        await products.UpdateAsync(product, cancellationToken);
        return Result.Success();
    }
}
