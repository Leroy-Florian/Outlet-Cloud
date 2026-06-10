using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IApiMetricRepository"/>.</summary>
public sealed class EfApiMetricRepository(CrmDbContext db) : IApiMetricRepository
{
    public async Task<IReadOnlyList<ApiMetricSample>> ListSinceAsync(
        ProductId productId,
        DateTime since,
        CancellationToken cancellationToken = default) =>
        await db.ApiMetricSamples
            .AsNoTracking()
            .Where(s => s.ProductId == productId && s.OccurredAt >= since)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ApiMetricSample sample, CancellationToken cancellationToken = default)
    {
        db.ApiMetricSamples.Add(sample);
        await db.SaveChangesAsync(cancellationToken);
    }
}
