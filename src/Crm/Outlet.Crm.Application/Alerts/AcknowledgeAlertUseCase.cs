using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Alerts;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Alerts;

public sealed record AcknowledgeAlertCommand(AlertId AlertId);

/// <summary>Marks an alert as seen so it leaves the unacknowledged inbox.</summary>
public sealed class AcknowledgeAlertUseCase(IAlertRepository alerts) : IUseCase<AcknowledgeAlertCommand>
{
    public async Task<Result> HandleAsync(AcknowledgeAlertCommand command, CancellationToken cancellationToken = default)
    {
        var alert = await alerts.GetByIdAsync(command.AlertId, cancellationToken);
        if (alert is null)
        {
            return Result.Failure(AlertErrors.NotFound(command.AlertId));
        }

        var acknowledged = alert.Acknowledge();
        if (acknowledged.IsFailure)
        {
            return acknowledged;
        }

        await alerts.UpdateAsync(alert, cancellationToken);
        return Result.Success();
    }
}
