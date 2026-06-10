using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Payments;

public sealed record RecordPaymentCommand(
    Guid ProductId,
    Guid? OrganizationId,
    decimal Amount,
    string Currency,
    string Source,
    string ExternalReference);

/// <summary>Records a pending payment coming from an external provider (Stripe, GitHub Sponsors…).</summary>
public sealed class RecordPaymentUseCase(
    IPaymentRepository payments,
    IProductRepository products,
    IOrganizationRepository organizations,
    ICurrentDateTimeProvider clock)
    : IUseCase<RecordPaymentCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RecordPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound(productId));
        }

        OrganizationId? organizationId = null;
        if (command.OrganizationId is { } rawOrganizationId)
        {
            organizationId = new OrganizationId(rawOrganizationId);
            if (await organizations.GetByIdAsync(organizationId.Value, cancellationToken) is null)
            {
                return Result.Failure<Guid>(OrganizationErrors.NotFound(organizationId.Value));
            }
        }

        var money = Money.Create(command.Amount, command.Currency);
        if (money.IsFailure)
        {
            return Result.Failure<Guid>(money.Error!);
        }

        var payment = Payment.Create(productId, organizationId, money.Value!, command.Source, command.ExternalReference, clock.UtcNow);
        if (payment.IsFailure)
        {
            return Result.Failure<Guid>(payment.Error!);
        }

        await payments.AddAsync(payment.Value!, cancellationToken);

        return Result.Success(payment.Value!.Id);
    }
}
