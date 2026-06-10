using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakeAlertRepository : IAlertRepository
{
    public List<Alert> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Alert?> GetByIdAsync(AlertId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Alert>>(Items);

    public Task<IReadOnlyList<Alert>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Alert>>([.. Items.Where(a => a.ProductId == productId)]);

    public Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        Items.Add(alert);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}
