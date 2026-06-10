using Outlet.Crm.Domain.Invoices;

namespace Outlet.Crm.Application.Ports;

/// <summary>
/// SECONDARY PORT — persistence of <see cref="Invoice"/> aggregates, including
/// the allocation of sequential invoice numbers per calendar year.
/// </summary>
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> ListAsync(InvoiceStatus? status = null, CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Next sequence number (1-based) for the given year. V1 LIMITATION: this is a
    /// read-then-insert allocation without a database-side sequence or unique
    /// constraint retry — two concurrent creations could race to the same number.
    /// Acceptable for a single-operator back office; revisit before multi-user billing.
    /// </summary>
    Task<int> NextSequenceAsync(int year, CancellationToken cancellationToken = default);
}
