using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Payments;

/// <summary>
/// Provider-agnostic payment record. Provider specifics (Stripe, GitHub Sponsors…)
/// stay outside the domain: only the source name and an external reference are kept,
/// in the same spirit as Outlet's port/adapter separation.
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    private Payment(
        Guid id,
        ProductId productId,
        OrganizationId? organizationId,
        Money amount,
        string source,
        string externalReference,
        DateTime createdAt)
        : base(id)
    {
        ProductId = productId;
        OrganizationId = organizationId;
        Amount = amount;
        Source = source;
        ExternalReference = externalReference;
        CreatedAt = createdAt;
        Status = PaymentStatus.Pending;
    }

    public ProductId ProductId { get; }

    public OrganizationId? OrganizationId { get; }

    public Money Amount { get; }

    public string Source { get; }

    public string ExternalReference { get; }

    public PaymentStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public static Result<Payment> Create(
        ProductId productId,
        OrganizationId? organizationId,
        Money amount,
        string source,
        string externalReference,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Result.Failure<Payment>(PaymentErrors.SourceRequired);
        }

        return Result.Success(new Payment(
            Guid.NewGuid(), productId, organizationId, amount, source.Trim(), externalReference.Trim(), createdAt));
    }

    public Result Settle()
    {
        if (Status is not PaymentStatus.Pending)
        {
            return Result.Failure(PaymentErrors.NotPending);
        }

        Status = PaymentStatus.Settled;
        return Result.Success();
    }

    public Result Fail()
    {
        if (Status is not PaymentStatus.Pending)
        {
            return Result.Failure(PaymentErrors.NotPending);
        }

        Status = PaymentStatus.Failed;
        return Result.Success();
    }

    public Result Refund()
    {
        if (Status is not PaymentStatus.Settled)
        {
            return Result.Failure(PaymentErrors.NotSettled);
        }

        Status = PaymentStatus.Refunded;
        return Result.Success();
    }
}
