using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Payments;

public sealed record ProcessBillingEventCommand(
    string ExternalReference,
    Guid ProductId,
    decimal Amount,
    string Currency,
    bool? IsRecurring = null,
    string? Status = null);

public enum BillingEventOutcome
{
    Recorded = 0,
    AlreadyProcessed = 1,
    Refunded = 2,
}

/// <summary>
/// Bridge from an external billing platform (Stripe-like) into the CRM: each webhook
/// event becomes a payment mutation. Idempotent on <see cref="ProcessBillingEventCommand.ExternalReference"/> —
/// replaying the same event is a successful no-op (<see cref="BillingEventOutcome.AlreadyProcessed"/>).
/// </summary>
public sealed class ProcessBillingEventUseCase(
    IPaymentRepository payments,
    IProductRepository products,
    ICurrentDateTimeProvider clock)
    : IUseCase<ProcessBillingEventCommand, BillingEventOutcome>
{
    private const string Source = "billing-webhook";
    private const string PaidStatus = "paid";
    private const string PendingStatus = "pending";
    private const string RefundedStatus = "refunded";

    public async Task<Result<BillingEventOutcome>> HandleAsync(
        ProcessBillingEventCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ExternalReference))
        {
            return Result.Failure<BillingEventOutcome>(PaymentErrors.ExternalReferenceRequired);
        }

        var externalReference = command.ExternalReference.Trim();
        var status = string.IsNullOrWhiteSpace(command.Status) ? PaidStatus : command.Status.Trim().ToLowerInvariant();
        if (status is not (PaidStatus or PendingStatus or RefundedStatus))
        {
            return Result.Failure<BillingEventOutcome>(PaymentErrors.UnknownBillingStatus(status));
        }

        var existing = await payments.FindByExternalReferenceAsync(externalReference, cancellationToken);

        if (status is RefundedStatus)
        {
            return await RefundAsync(existing, externalReference, cancellationToken);
        }

        if (existing is not null)
        {
            return Result.Success(BillingEventOutcome.AlreadyProcessed);
        }

        return await RecordAsync(command, externalReference, settle: status is PaidStatus, cancellationToken);
    }

    private async Task<Result<BillingEventOutcome>> RefundAsync(
        Payment? existing,
        string externalReference,
        CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return Result.Failure<BillingEventOutcome>(PaymentErrors.NotFoundByReference(externalReference));
        }

        if (existing.Status is PaymentStatus.Refunded)
        {
            return Result.Success(BillingEventOutcome.AlreadyProcessed);
        }

        var refunded = existing.Refund();
        if (refunded.IsFailure)
        {
            return Result.Failure<BillingEventOutcome>(refunded.Error!);
        }

        await payments.UpdateAsync(existing, cancellationToken);
        return Result.Success(BillingEventOutcome.Refunded);
    }

    private async Task<Result<BillingEventOutcome>> RecordAsync(
        ProcessBillingEventCommand command,
        string externalReference,
        bool settle,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<BillingEventOutcome>(ProductErrors.NotFound(productId));
        }

        var money = Money.Create(command.Amount, command.Currency);
        if (money.IsFailure)
        {
            return Result.Failure<BillingEventOutcome>(money.Error!);
        }

        var payment = Payment.Create(
            productId, null, money.Value!, Source, externalReference, clock.UtcNow, command.IsRecurring ?? false);
        if (payment.IsFailure)
        {
            return Result.Failure<BillingEventOutcome>(payment.Error!);
        }

        if (settle)
        {
            var settled = payment.Value!.Settle();
            if (settled.IsFailure)
            {
                return Result.Failure<BillingEventOutcome>(settled.Error!);
            }
        }

        await payments.AddAsync(payment.Value!, cancellationToken);
        return Result.Success(BillingEventOutcome.Recorded);
    }
}
