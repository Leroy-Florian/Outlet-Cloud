using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Payments;

namespace Outlet.Crm.Infrastructure.Persistence.Repositories;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IPaymentRepository"/>.</summary>
public sealed class EfPaymentRepository(CrmDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> FindByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default) =>
        db.Payments.FirstOrDefaultAsync(p => p.ExternalReference == externalReference, cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Payments.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
