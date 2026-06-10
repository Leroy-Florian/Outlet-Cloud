using Outlet.Crm.Domain.Payments;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Invoices;

/// <summary>What a caller asks to bill; validated when the invoice is created.</summary>
public sealed record InvoiceLineDraft(string Description, decimal Quantity, Money UnitPrice);

/// <summary>One billed line, owned by its <see cref="Invoice"/> (never referenced from outside).</summary>
public sealed class InvoiceLine(Guid id, string description, decimal quantity, Money unitPrice) : Entity<Guid>(id)
{
    public string Description { get; } = description;

    public decimal Quantity { get; } = quantity;

    public Money UnitPrice { get; } = unitPrice;

    public decimal LineTotal => Quantity * UnitPrice.Amount;
}
