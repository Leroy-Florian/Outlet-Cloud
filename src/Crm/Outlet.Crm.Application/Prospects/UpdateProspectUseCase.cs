using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Prospects;

public sealed record UpdateProspectCommand(
    ProspectId ProspectId,
    decimal? EstimatedValue,
    string? EstimatedValueCurrency,
    string? Company);

/// <summary>
/// PATCH of the qualification fields: replaces the estimated deal value and the
/// company (null clears). Closed (Won/Lost) prospects are immutable.
/// </summary>
public sealed class UpdateProspectUseCase(IProspectRepository prospects) : IUseCase<UpdateProspectCommand>
{
    public async Task<Result> HandleAsync(UpdateProspectCommand command, CancellationToken cancellationToken = default)
    {
        var prospect = await prospects.GetByIdAsync(command.ProspectId, cancellationToken);
        if (prospect is null)
        {
            return Result.Failure(ProspectErrors.NotFound(command.ProspectId));
        }

        var estimatedValue = ProspectEstimatedValue.Resolve(command.EstimatedValue, command.EstimatedValueCurrency);
        if (estimatedValue.IsFailure)
        {
            return Result.Failure(estimatedValue.Error!);
        }

        var updated = prospect.UpdateDetails(estimatedValue.Value, command.Company);
        if (updated.IsFailure)
        {
            return updated;
        }

        await prospects.UpdateAsync(prospect, cancellationToken);
        return Result.Success();
    }
}
