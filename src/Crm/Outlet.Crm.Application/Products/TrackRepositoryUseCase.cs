using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Products;

public sealed record TrackRepositoryCommand(Guid ProductId, string Repository);

/// <summary>Adds a GitHub repository to the set tracked by a product.</summary>
public sealed class TrackRepositoryUseCase(IProductRepository products) : IUseCase<TrackRepositoryCommand>
{
    public async Task<Result> HandleAsync(TrackRepositoryCommand command, CancellationToken cancellationToken = default)
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

        var tracked = product.TrackRepository(repository.Value!);
        if (tracked.IsFailure)
        {
            return tracked;
        }

        await products.UpdateAsync(product, cancellationToken);
        return Result.Success();
    }
}
