using Microsoft.EntityFrameworkCore;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="ISubscriptionRepository"/>.</summary>
public sealed class EfSubscriptionRepository(CloudDbContext db) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByAccountAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        var record = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.AccountId == accountId.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        db.Subscriptions.Add(ToRecord(subscription));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        var record = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscription.Id.Value, cancellationToken);

        if (record is null)
        {
            db.Subscriptions.Add(ToRecord(subscription));
        }
        else
        {
            record.Plan = subscription.Plan.ToString();
            record.Status = subscription.Status.ToString();
            record.TrialStartedOn = subscription.Trial?.StartedOn;
            record.TrialEndsOn = subscription.Trial?.EndsOn;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Subscription ToDomain(SubscriptionRecord record)
    {
        var trial = record is { TrialStartedOn: { } start, TrialEndsOn: { } end }
            ? TrialPeriod.Between(start, end)
            : null;

        return Subscription.Restore(
            SubscriptionId.From(record.Id),
            AccountId.From(record.AccountId),
            Enum.Parse<PlanTier>(record.Plan),
            Enum.Parse<SubscriptionStatus>(record.Status),
            trial);
    }

    private static SubscriptionRecord ToRecord(Subscription subscription) =>
        new()
        {
            Id = subscription.Id.Value,
            AccountId = subscription.AccountId.Value,
            Plan = subscription.Plan.ToString(),
            Status = subscription.Status.ToString(),
            TrialStartedOn = subscription.Trial?.StartedOn,
            TrialEndsOn = subscription.Trial?.EndsOn,
        };
}
