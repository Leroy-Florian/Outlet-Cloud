using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Invoices;

public sealed record GetInvoicesQuery(InvoiceStatus? Status = null);

/// <summary>Lists invoices, optionally filtered by status.</summary>
public sealed class GetInvoicesUseCase(IInvoiceRepository invoices)
    : IUseCase<GetInvoicesQuery, IReadOnlyList<Invoice>>
{
    public async Task<Result<IReadOnlyList<Invoice>>> HandleAsync(
        GetInvoicesQuery command,
        CancellationToken cancellationToken = default) =>
        Result.Success(await invoices.ListAsync(command.Status, cancellationToken));
}
