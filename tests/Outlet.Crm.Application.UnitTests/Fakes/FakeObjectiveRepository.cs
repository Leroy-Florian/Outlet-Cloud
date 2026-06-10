using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakeObjectiveRepository : IObjectiveRepository
{
    public List<Objective> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Objective?> GetByIdAsync(ObjectiveId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(o => o.Id == id));

    public Task<Objective?> FindAsync(ProductId? productId, ObjectiveMetric metric, DateOnly month, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(o => o.ProductId == productId && o.Metric == metric && o.Month == month));

    public Task<IReadOnlyList<Objective>> ListByMonthAsync(DateOnly month, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Objective>>([.. Items.Where(o => o.Month == month)]);

    public Task AddAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        Items.Add(objective);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        Items.Remove(objective);
        return Task.CompletedTask;
    }
}
