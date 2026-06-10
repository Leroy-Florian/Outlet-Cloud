using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record UntrackRepositoryCommand(Guid ProductId, string Repository);

/// <summary>Removes a GitHub repository from the set tracked by a product.</summary>
public sealed class UntrackRepositoryUseCase(IProductRepository products) : IUseCase<UntrackRepositoryCommand>
{
    public async Task<Result> HandleAsync(UntrackRepositoryCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        var repository = RepositoryName.Create(command.Repository);
        if (repository.IsFailure)
        {
            return Result.Failure(repository.Error!);
        }

        var untracked = product.UntrackRepository(repository.Value!);
        if (untracked.IsFailure)
        {
            return untracked;
        }

        await products.UpdateAsync(product, cancellationToken);
        return Result.Success();
    }
}
