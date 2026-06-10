using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Objectives;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Objectives;

public sealed record DeleteObjectiveCommand(ObjectiveId Id);

/// <summary>Removes a monthly objective; deleting an unknown id is an error, not a no-op.</summary>
public sealed class DeleteObjectiveUseCase(IObjectiveRepository objectives)
    : IUseCase<DeleteObjectiveCommand>
{
    public async Task<Result> HandleAsync(DeleteObjectiveCommand command, CancellationToken cancellationToken = default)
    {
        var objective = await objectives.GetByIdAsync(command.Id, cancellationToken);
        if (objective is null)
        {
            return Result.Failure(ObjectiveErrors.NotFound(command.Id));
        }

        await objectives.RemoveAsync(objective, cancellationToken);
        return Result.Success();
    }
}
