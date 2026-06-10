using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="Objective"/> aggregates.</summary>
public interface IObjectiveRepository
{
    Task<Objective?> GetByIdAsync(ObjectiveId id, CancellationToken cancellationToken = default);

    /// <summary>The unique objective for a (product, metric, month) triple, if any.</summary>
    Task<Objective?> FindAsync(ProductId? productId, ObjectiveMetric metric, DateOnly month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Objective>> ListByMonthAsync(DateOnly month, CancellationToken cancellationToken = default);

    Task AddAsync(Objective objective, CancellationToken cancellationToken = default);

    Task UpdateAsync(Objective objective, CancellationToken cancellationToken = default);

    Task RemoveAsync(Objective objective, CancellationToken cancellationToken = default);
}
