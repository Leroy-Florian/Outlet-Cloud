using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Alerts;

/// <summary>
/// One triggered alert for a product (downloads spike/drop, stars milestone,
/// snapshot failure). V1 deliberately has no AlertRule aggregate: the rules
/// and thresholds live as constants in <see cref="AlertRules"/>, alerts are
/// just the triggered facts, acknowledgeable from the dashboard.
/// </summary>
public sealed class Alert : AggregateRoot<AlertId>
{
    private Alert(
        AlertId id,
        ProductId productId,
        AlertType type,
        string message,
        DateTime triggeredAt)
        : base(id)
    {
        ProductId = productId;
        Type = type;
        Message = message;
        TriggeredAt = triggeredAt;
    }

    public ProductId ProductId { get; }

    public AlertType Type { get; }

    public string Message { get; }

    public DateTime TriggeredAt { get; }

    public bool Acknowledged { get; private set; }

    public static Result<Alert> Create(ProductId productId, AlertType type, string message, DateTime triggeredAt)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Failure<Alert>(AlertErrors.MessageRequired);
        }

        return Result.Success(new Alert(AlertId.New(), productId, type, message.Trim(), triggeredAt));
    }

    public Result Acknowledge()
    {
        if (Acknowledged)
        {
            return Result.Failure(AlertErrors.AlreadyAcknowledged);
        }

        Acknowledged = true;
        return Result.Success();
    }
}
