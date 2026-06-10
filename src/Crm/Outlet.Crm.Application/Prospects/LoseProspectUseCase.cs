using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Prospects;

public sealed record LoseProspectCommand(ProspectId ProspectId, string Reason);

/// <summary>Closes an open prospect as Lost with a mandatory reason.</summary>
public sealed class LoseProspectUseCase(IProspectRepository prospects) : IUseCase<LoseProspectCommand>
{
    public async Task<Result> HandleAsync(LoseProspectCommand command, CancellationToken cancellationToken = default)
    {
        var prospect = await prospects.GetByIdAsync(command.ProspectId, cancellationToken);
        if (prospect is null)
        {
            return Result.Failure(ProspectErrors.NotFound(command.ProspectId));
        }

        var lost = prospect.Lose(command.Reason);
        if (lost.IsFailure)
        {
            return lost;
        }

        await prospects.UpdateAsync(prospect, cancellationToken);
        return Result.Success();
    }
}
