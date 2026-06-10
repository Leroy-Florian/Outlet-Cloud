using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="ITrafficSampleRepository"/>.</summary>
public sealed class EfTrafficSampleRepository(CrmDbContext db) : ITrafficSampleRepository
{
    public async Task<IReadOnlyList<TrafficSample>> ListSinceAsync(
        ProductId productId,
        DateTime since,
        CancellationToken cancellationToken = default) =>
        await db.TrafficSamples
            .AsNoTracking()
            .Where(s => s.ProductId == productId && s.OccurredAt >= since)
            .OrderBy(s => s.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TrafficSample sample, CancellationToken cancellationToken = default)
    {
        db.TrafficSamples.Add(sample);
        await db.SaveChangesAsync(cancellationToken);
    }
}
