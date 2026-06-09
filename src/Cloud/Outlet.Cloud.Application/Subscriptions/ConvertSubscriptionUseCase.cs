using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Subscriptions;

/// <summary>Command: convert an account's trial to a paying Pro plan (mock/real checkout upstream).</summary>
public sealed record ConvertSubscriptionCommand(Guid AccountId);

/// <summary>
/// Converts a trialing subscription to Active. The actual billing handshake (Stripe…) plugs in
/// upstream of this use case; here we only flip the domain state once payment is secured.
/// </summary>
public sealed class ConvertSubscriptionUseCase(ISubscriptionRepository subscriptions)
    : IUseCase<ConvertSubscriptionCommand>
{
    public async Task<Result> HandleAsync(ConvertSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var accountResult = Guard.TryBuild(() => AccountId.From(command.AccountId), "Account id is invalid.");
        if (accountResult.IsFailure)
            return Result.Failure(accountResult.Error!);

        var subscription = await subscriptions.GetByAccountAsync(accountResult.Value!, cancellationToken);
        if (subscription is null)
            return Result.Failure("This account has no subscription to convert.");

        var conversion = subscription.Convert(PlanTier.Pro);
        if (conversion.IsFailure)
            return conversion;

        await subscriptions.UpdateAsync(subscription, cancellationToken);

        return Result.Success();
    }
}
