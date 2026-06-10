using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Payments;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Payments;

public sealed record SettlePaymentCommand(Guid PaymentId);

/// <summary>Marks a pending payment as settled.</summary>
public sealed class SettlePaymentUseCase(IPaymentRepository payments) : IUseCase<SettlePaymentCommand>
{
    public async Task<Result> HandleAsync(SettlePaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await payments.GetByIdAsync(command.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure(PaymentErrors.NotFound(command.PaymentId));
        }

        var settled = payment.Settle();
        if (settled.IsFailure)
        {
            return settled;
        }

        await payments.UpdateAsync(payment, cancellationToken);
        return Result.Success();
    }
}
