using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Invoices;

public sealed record MarkInvoicePaidCommand(InvoiceId Id, Guid? PaymentId = null);

/// <summary>
/// Moves an Issued invoice to Paid. The optional payment id is stored as a loose
/// reference only — recording the payment itself stays a separate concern
/// (<c>RecordPaymentUseCase</c>), keeping invoicing and payments decoupled.
/// </summary>
public sealed class MarkInvoicePaidUseCase(
    IInvoiceRepository invoices,
    ICurrentDateTimeProvider clock)
    : IUseCase<MarkInvoicePaidCommand>
{
    public async Task<Result> HandleAsync(MarkInvoicePaidCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(command.Id, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure(InvoiceErrors.NotFound(command.Id));
        }

        var paid = invoice.MarkPaid(clock.UtcNow, command.PaymentId);
        if (paid.IsFailure)
        {
            return paid;
        }

        await invoices.UpdateAsync(invoice, cancellationToken);
        return Result.Success();
    }
}
