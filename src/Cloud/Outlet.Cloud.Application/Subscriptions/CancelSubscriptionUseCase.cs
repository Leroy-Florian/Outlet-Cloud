using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Subscriptions;

/// <summary>Command: cancel an account's active plan (it becomes read-only, data retained).</summary>
public sealed record CancelSubscriptionCommand(Guid AccountId);

/// <summary>
/// Cancels an active subscription: the account moves to Suspended (read-only) rather than being
/// deleted, mirroring the trial-expiry path so the retention/purge timeline is identical.
/// </summary>
public sealed class CancelSubscriptionUseCase(ISubscriptionRepository subscriptions)
    : IUseCase<CancelSubscriptionCommand>
{
    public async Task<Result> HandleAsync(CancelSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var accountResult = Guard.TryBuild(() => AccountId.From(command.AccountId), "Account id is invalid.");
        if (accountResult.IsFailure)
            return Result.Failure(accountResult.Error!);

        var subscription = await subscriptions.GetByAccountAsync(accountResult.Value!, cancellationToken);
        if (subscription is null)
            return Result.Failure("This account has no subscription to cancel.");

        var cancellation = subscription.Cancel();
        if (cancellation.IsFailure)
            return cancellation;

        await subscriptions.UpdateAsync(subscription, cancellationToken);

        return Result.Success();
    }
}
