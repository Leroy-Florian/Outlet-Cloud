using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="Alert"/> aggregates.</summary>
public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(AlertId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Alert>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task AddAsync(Alert alert, CancellationToken cancellationToken = default);

    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);
}
