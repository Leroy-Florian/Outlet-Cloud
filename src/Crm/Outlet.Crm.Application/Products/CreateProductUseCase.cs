using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record CreateProductCommand(string Name, string? Description);

/// <summary>Creates a <see cref="Product"/> and persists it.</summary>
public sealed class CreateProductUseCase(IProductRepository products, ICurrentDateTimeProvider clock)
    : IUseCase<CreateProductCommand, ProductId>
{
    public async Task<Result<ProductId>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = Product.Create(command.Name, command.Description, clock.UtcNow);
        if (product.IsFailure)
        {
            return Result.Failure<ProductId>(product.Error!);
        }

        await products.AddAsync(product.Value!, cancellationToken);

        return Result.Success(product.Value!.Id);
    }
}
