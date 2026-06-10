using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Objectives;

/// <summary>
/// A monthly target on one CRM metric, either scoped to a product or global
/// (<see cref="ProductId"/> null). The month is normalized to its first day;
/// uniqueness of (product, metric, month) is enforced by the upsert use case
/// against the repository.
/// </summary>
public sealed class Objective : AggregateRoot<ObjectiveId>
{
    private Objective(
        ObjectiveId id,
        ProductId? productId,
        ObjectiveMetric metric,
        decimal targetValue,
        DateOnly month,
        DateTime createdAt)
        : base(id)
    {
        ProductId = productId;
        Metric = metric;
        TargetValue = targetValue;
        Month = month;
        CreatedAt = createdAt;
    }

    /// <summary>Null = a global objective spanning every product.</summary>
    public ProductId? ProductId { get; }

    public ObjectiveMetric Metric { get; }

    public decimal TargetValue { get; private set; }

    /// <summary>First day of the targeted month.</summary>
    public DateOnly Month { get; }

    public DateTime CreatedAt { get; }

    public static Result<Objective> Create(
        ProductId? productId,
        ObjectiveMetric metric,
        decimal targetValue,
        DateOnly month,
        DateTime createdAt)
    {
        if (targetValue <= 0)
        {
            return Result.Failure<Objective>(ObjectiveErrors.TargetNotPositive);
        }

        return Result.Success(new Objective(
            ObjectiveId.New(), productId, metric, targetValue, new DateOnly(month.Year, month.Month, 1), createdAt));
    }

    public Result UpdateTarget(decimal targetValue)
    {
        if (targetValue <= 0)
        {
            return Result.Failure(ObjectiveErrors.TargetNotPositive);
        }

        TargetValue = targetValue;
        return Result.Success();
    }

    /// <summary>Raw progress percentage (one decimal), deliberately uncapped — the frontend clips display.</summary>
    public decimal ProgressPercent(decimal actualValue) =>
        Math.Round(actualValue * 100m / TargetValue, 1);
}
