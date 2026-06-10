using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Invoices;

/// <summary>
/// Lightweight invoice, v1: data only (PDF rendering is out of scope — a later
/// FluentPdf dogfooding candidate). State machine: Draft → Issued → Paid, with
/// Cancelled reachable from Draft or Issued; Paid and Cancelled are terminal.
/// The payment link is a loose <see cref="PaymentId"/> reference: invoicing
/// stays decoupled from payment recording.
/// </summary>
public sealed class Invoice : AggregateRoot<InvoiceId>
{
    private readonly List<InvoiceLine> _lines = [];

    private Invoice(
        InvoiceId id,
        string invoiceNumber,
        string customerName,
        Email? customerEmail,
        string? customerAddress,
        DateTime createdAt)
        : base(id)
    {
        InvoiceNumber = invoiceNumber;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CustomerAddress = customerAddress;
        CreatedAt = createdAt;
        Status = InvoiceStatus.Draft;
    }

    /// <summary>Sequential human number, e.g. "INV-2026-0001" (allocated by the repository port).</summary>
    public string InvoiceNumber { get; }

    public string CustomerName { get; }

    public Email? CustomerEmail { get; }

    public string? CustomerAddress { get; }

    public InvoiceStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? IssuedAt { get; private set; }

    public DateTime? PaidAt { get; private set; }

    /// <summary>Optional loose reference to a recorded payment; set when marked paid.</summary>
    public Guid? PaymentId { get; private set; }

    public IReadOnlyList<InvoiceLine> Lines => _lines;

    /// <summary>All lines share this currency (enforced at creation).</summary>
    public string Currency => _lines[0].UnitPrice.Currency;

    public decimal Total => _lines.Sum(l => l.LineTotal);

    public static Result<Invoice> Create(
        string invoiceNumber,
        string customerName,
        Email? customerEmail,
        string? customerAddress,
        IReadOnlyList<InvoiceLineDraft> lines,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return Result.Failure<Invoice>(InvoiceErrors.NumberRequired);
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return Result.Failure<Invoice>(InvoiceErrors.CustomerNameRequired);
        }

        if (lines.Count is 0)
        {
            return Result.Failure<Invoice>(InvoiceErrors.LinesRequired);
        }

        if (lines.Any(l => string.IsNullOrWhiteSpace(l.Description)))
        {
            return Result.Failure<Invoice>(InvoiceErrors.LineDescriptionRequired);
        }

        if (lines.Any(l => l.Quantity <= 0))
        {
            return Result.Failure<Invoice>(InvoiceErrors.LineQuantityNotPositive);
        }

        if (lines.Select(l => l.UnitPrice.Currency).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            return Result.Failure<Invoice>(InvoiceErrors.MixedCurrencies);
        }

        var trimmedAddress = customerAddress?.Trim();
        var invoice = new Invoice(
            InvoiceId.New(),
            invoiceNumber.Trim(),
            customerName.Trim(),
            customerEmail,
            string.IsNullOrEmpty(trimmedAddress) ? null : trimmedAddress,
            createdAt);

        invoice._lines.AddRange(lines.Select(l => new InvoiceLine(Guid.NewGuid(), l.Description.Trim(), l.Quantity, l.UnitPrice)));

        return Result.Success(invoice);
    }

    public Result Issue(DateTime issuedAt)
    {
        if (Status is not InvoiceStatus.Draft)
        {
            return Result.Failure(InvoiceErrors.InvalidTransition(Status, InvoiceStatus.Issued));
        }

        Status = InvoiceStatus.Issued;
        IssuedAt = issuedAt;
        return Result.Success();
    }

    public Result MarkPaid(DateTime paidAt, Guid? paymentId)
    {
        if (Status is not InvoiceStatus.Issued)
        {
            return Result.Failure(InvoiceErrors.InvalidTransition(Status, InvoiceStatus.Paid));
        }

        Status = InvoiceStatus.Paid;
        PaidAt = paidAt;
        PaymentId = paymentId;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Issued))
        {
            return Result.Failure(InvoiceErrors.InvalidTransition(Status, InvoiceStatus.Cancelled));
        }

        Status = InvoiceStatus.Cancelled;
        return Result.Success();
    }
}
