using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Objectives;

public sealed record SetObjectiveCommand(
    Guid? ProductId,
    ObjectiveMetric Metric,
    string Month,
    decimal TargetValue);

/// <summary>
/// Upserts the monthly target for a (product, metric, month) triple: creates the
/// objective on first call, updates the target afterwards — this is also what
/// enforces the uniqueness of the triple.
/// </summary>
public sealed class SetObjectiveUseCase(
    IObjectiveRepository objectives,
    IProductRepository products,
    ICurrentDateTimeProvider clock)
    : IUseCase<SetObjectiveCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(SetObjectiveCommand command, CancellationToken cancellationToken = default)
    {
        var month = ObjectiveMonth.Parse(command.Month);
        if (month.IsFailure)
        {
            return Result.Failure<Guid>(month.Error!);
        }

        ProductId? productId = null;
        if (command.ProductId is { } rawProductId)
        {
            productId = new ProductId(rawProductId);
            if (await products.GetByIdAsync(productId.Value, cancellationToken) is null)
            {
                return Result.Failure<Guid>(ProductErrors.NotFound(productId.Value));
            }
        }

        var existing = await objectives.FindAsync(productId, command.Metric, month.Value, cancellationToken);
        if (existing is not null)
        {
            var updated = existing.UpdateTarget(command.TargetValue);
            if (updated.IsFailure)
            {
                return Result.Failure<Guid>(updated.Error!);
            }

            await objectives.UpdateAsync(existing, cancellationToken);
            return Result.Success(existing.Id.Value);
        }

        var objective = Objective.Create(productId, command.Metric, command.TargetValue, month.Value, clock.UtcNow);
        if (objective.IsFailure)
        {
            return Result.Failure<Guid>(objective.Error!);
        }

        await objectives.AddAsync(objective.Value!, cancellationToken);
        return Result.Success(objective.Value!.Id.Value);
    }
}
