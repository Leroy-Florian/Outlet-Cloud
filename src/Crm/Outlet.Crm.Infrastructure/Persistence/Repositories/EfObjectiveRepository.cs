using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IObjectiveRepository"/>.</summary>
public sealed class EfObjectiveRepository(CrmDbContext db) : IObjectiveRepository
{
    public Task<Objective?> GetByIdAsync(ObjectiveId id, CancellationToken cancellationToken = default) =>
        db.Objectives.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Objective?> FindAsync(ProductId? productId, ObjectiveMetric metric, DateOnly month, CancellationToken cancellationToken = default) =>
        db.Objectives.FirstOrDefaultAsync(
            o => o.ProductId == productId && o.Metric == metric && o.Month == month, cancellationToken);

    public async Task<IReadOnlyList<Objective>> ListByMonthAsync(DateOnly month, CancellationToken cancellationToken = default) =>
        await db.Objectives.AsNoTracking().Where(o => o.Month == month).ToListAsync(cancellationToken);

    public async Task AddAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        db.Objectives.Add(objective);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Objective objective, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task RemoveAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        db.Objectives.Remove(objective);
        await db.SaveChangesAsync(cancellationToken);
    }
}
