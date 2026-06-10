using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public List<Invoice> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<IReadOnlyList<Invoice>> ListAsync(InvoiceStatus? status = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>([.. Items.Where(i => status is null || i.Status == status)]);

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        Items.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }

    public Task<int> NextSequenceAsync(int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{year:D4}-";
        var max = Items
            .Where(i => i.InvoiceNumber.StartsWith(prefix, StringComparison.Ordinal))
            .Select(i => int.Parse(i.InvoiceNumber[prefix.Length..]))
            .DefaultIfEmpty(0)
            .Max();

        return Task.FromResult(max + 1);
    }
}
