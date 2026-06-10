using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IProductRepository"/>.</summary>
public sealed class EfProductRepository(CrmDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
