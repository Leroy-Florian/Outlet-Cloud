using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Alerts;

public sealed record GetAlertsQuery(Guid? ProductId = null, bool? Acknowledged = null);

/// <summary>
/// Alert inbox: optionally filtered by product and acknowledged flag,
/// unacknowledged first, then most recently triggered first.
/// </summary>
public sealed class GetAlertsUseCase(
    IAlertRepository alerts,
    IProductRepository products)
    : IUseCase<GetAlertsQuery, IReadOnlyList<Alert>>
{
    public async Task<Result<IReadOnlyList<Alert>>> HandleAsync(GetAlertsQuery command, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Alert> all;
        if (command.ProductId is { } rawProductId)
        {
            var productId = new ProductId(rawProductId);
            if (await products.GetByIdAsync(productId, cancellationToken) is null)
            {
                return Result.Failure<IReadOnlyList<Alert>>(ProductErrors.NotFound(productId));
            }

            all = await alerts.ListByProductAsync(productId, cancellationToken);
        }
        else
        {
            all = await alerts.ListAsync(cancellationToken);
        }

        List<Alert> items = [.. all
            .Where(a => command.Acknowledged is null || a.Acknowledged == command.Acknowledged)
            .OrderBy(a => a.Acknowledged)
            .ThenByDescending(a => a.TriggeredAt)];

        return Result.Success<IReadOnlyList<Alert>>(items);
    }
}
