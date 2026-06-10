using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IInvoiceRepository"/>.</summary>
public sealed class EfInvoiceRepository(CrmDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken = default) =>
        db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Invoice>> ListAsync(InvoiceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = db.Invoices.AsNoTracking().Include(i => i.Lines);
        return status is { } wanted
            ? await query.Where(i => i.Status == wanted).OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken)
            : await query.OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<int> NextSequenceAsync(int year, CancellationToken cancellationToken = default)
    {
        // Numbers follow "INV-{year}-{seq:D4}". V1: max-suffix + 1 computed client-side
        // (see the port's documented single-operator concurrency limitation).
        var prefix = $"INV-{year:D4}-";
        var numbers = await db.Invoices.AsNoTracking()
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .Select(i => i.InvoiceNumber)
            .ToListAsync(cancellationToken);

        var max = numbers
            .Select(n => int.Parse(n[prefix.Length..], CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();

        return max + 1;
    }
}
