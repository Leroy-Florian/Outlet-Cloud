using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IFeedbackRepository"/>.</summary>
public sealed class EfFeedbackRepository(CrmDbContext db) : IFeedbackRepository
{
    public Task<Feedback?> GetByIdAsync(FeedbackId id, CancellationToken cancellationToken = default) =>
        db.FeedbackItems.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Feedback>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.FeedbackItems.AsNoTracking().OrderByDescending(f => f.ReceivedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Feedback>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        await db.FeedbackItems
            .AsNoTracking()
            .Where(f => f.ProductId == productId)
            .OrderByDescending(f => f.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default)
    {
        db.FeedbackItems.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Feedback feedback, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
