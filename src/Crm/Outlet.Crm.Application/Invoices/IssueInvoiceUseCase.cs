using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Invoices;

public sealed record IssueInvoiceCommand(InvoiceId Id);

/// <summary>Moves a Draft invoice to Issued, stamping IssuedAt.</summary>
public sealed class IssueInvoiceUseCase(
    IInvoiceRepository invoices,
    ICurrentDateTimeProvider clock)
    : IUseCase<IssueInvoiceCommand>
{
    public async Task<Result> HandleAsync(IssueInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(command.Id, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure(InvoiceErrors.NotFound(command.Id));
        }

        var issued = invoice.Issue(clock.UtcNow);
        if (issued.IsFailure)
        {
            return issued;
        }

        await invoices.UpdateAsync(invoice, cancellationToken);
        return Result.Success();
    }
}
