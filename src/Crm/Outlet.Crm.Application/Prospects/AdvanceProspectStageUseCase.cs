using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Prospects;

public sealed record AdvanceProspectStageCommand(ProspectId ProspectId, ProspectStage Target);

/// <summary>Advances a prospect through the pipeline (forward only; Lost is always reachable).</summary>
public sealed class AdvanceProspectStageUseCase(IProspectRepository prospects) : IUseCase<AdvanceProspectStageCommand>
{
    public async Task<Result> HandleAsync(AdvanceProspectStageCommand command, CancellationToken cancellationToken = default)
    {
        var prospect = await prospects.GetByIdAsync(command.ProspectId, cancellationToken);
        if (prospect is null)
        {
            return Result.Failure(ProspectErrors.NotFound(command.ProspectId));
        }

        var advanced = prospect.Advance(command.Target);
        if (advanced.IsFailure)
        {
            return advanced;
        }

        await prospects.UpdateAsync(prospect, cancellationToken);
        return Result.Success();
    }
}
