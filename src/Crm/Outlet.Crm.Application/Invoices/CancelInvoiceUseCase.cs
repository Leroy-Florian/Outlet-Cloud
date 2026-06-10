using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Invoices;

public sealed record CancelInvoiceCommand(InvoiceId Id);

/// <summary>Cancels a Draft or Issued invoice; Paid invoices are immutable facts.</summary>
public sealed class CancelInvoiceUseCase(IInvoiceRepository invoices)
    : IUseCase<CancelInvoiceCommand>
{
    public async Task<Result> HandleAsync(CancelInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(command.Id, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure(InvoiceErrors.NotFound(command.Id));
        }

        var cancelled = invoice.Cancel();
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await invoices.UpdateAsync(invoice, cancellationToken);
        return Result.Success();
    }
}
