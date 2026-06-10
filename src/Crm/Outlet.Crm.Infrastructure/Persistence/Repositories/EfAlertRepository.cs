using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IAlertRepository"/>.</summary>
public sealed class EfAlertRepository(CrmDbContext db) : IAlertRepository
{
    public Task<Alert?> GetByIdAsync(AlertId id, CancellationToken cancellationToken = default) =>
        db.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Alerts.AsNoTracking().OrderByDescending(a => a.TriggeredAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Alert>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        await db.Alerts.AsNoTracking()
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
