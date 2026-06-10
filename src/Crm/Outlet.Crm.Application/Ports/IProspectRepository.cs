using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="Prospect"/> aggregates.</summary>
public interface IProspectRepository
{
    Task<Prospect?> GetByIdAsync(ProspectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Prospect prospect, CancellationToken cancellationToken = default);

    Task UpdateAsync(Prospect prospect, CancellationToken cancellationToken = default);
}
