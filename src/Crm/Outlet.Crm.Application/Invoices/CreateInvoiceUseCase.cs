using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Invoices;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Invoices;

public sealed record InvoiceLineRequest(string Description, decimal Quantity, decimal UnitPrice, string Currency);

public sealed record CreateInvoiceCommand(
    string CustomerName,
    string? CustomerEmail,
    string? CustomerAddress,
    IReadOnlyList<InvoiceLineRequest> Lines);

public sealed record CreatedInvoice(Guid Id, string InvoiceNumber);

/// <summary>
/// Creates a Draft invoice with a sequential per-year number ("INV-2026-0001")
/// allocated through <see cref="IInvoiceRepository.NextSequenceAsync"/>.
/// </summary>
public sealed class CreateInvoiceUseCase(
    IInvoiceRepository invoices,
    ICurrentDateTimeProvider clock)
    : IUseCase<CreateInvoiceCommand, CreatedInvoice>
{
    public async Task<Result<CreatedInvoice>> HandleAsync(CreateInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.CustomerEmail))
        {
            var parsed = Email.Create(command.CustomerEmail);
            if (parsed.IsFailure)
            {
                return Result.Failure<CreatedInvoice>(parsed.Error!);
            }

            email = parsed.Value;
        }

        List<InvoiceLineDraft> lines = [];
        foreach (var line in command.Lines)
        {
            var unitPrice = Money.Create(line.UnitPrice, line.Currency);
            if (unitPrice.IsFailure)
            {
                return Result.Failure<CreatedInvoice>(unitPrice.Error!);
            }

            lines.Add(new InvoiceLineDraft(line.Description, line.Quantity, unitPrice.Value!));
        }

        var year = clock.Today.Year;
        var sequence = await invoices.NextSequenceAsync(year, cancellationToken);
        var number = $"INV-{year:D4}-{sequence:D4}";

        var invoice = Invoice.Create(number, command.CustomerName, email, command.CustomerAddress, lines, clock.UtcNow);
        if (invoice.IsFailure)
        {
            return Result.Failure<CreatedInvoice>(invoice.Error!);
        }

        await invoices.AddAsync(invoice.Value!, cancellationToken);
        return Result.Success(new CreatedInvoice(invoice.Value!.Id.Value, number));
    }
}
