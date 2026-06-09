using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Subscriptions;

/// <summary>Command: start a frictionless Pro trial for an account (on sign-up).</summary>
public sealed record StartTrialCommand(Guid AccountId, string Email, int TrialDays);

/// <summary>
/// Starts a trial: validates inputs, enforces the (replaceable) anti-abuse policy, guarantees
/// one subscription per account, then seeds a <see cref="Subscription"/> in the Trialing state.
/// The trial window is anchored on the server clock, never on a client-supplied date.
/// </summary>
public sealed class StartTrialUseCase(
    ISubscriptionRepository subscriptions,
    ITrialEligibilityPolicy eligibility,
    ICurrentDateTimeProvider clock)
    : IUseCase<StartTrialCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(StartTrialCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TrialDays <= 0)
            return Result<Guid>.Failure("Trial duration must be a positive number of days.");

        var accountResult = Guard.TryBuild(() => AccountId.From(command.AccountId), "Account id is invalid.");
        if (accountResult.IsFailure)
            return Result<Guid>.Failure(accountResult.Error!);

        var eligible = await eligibility.EnsureEligibleAsync(command.Email, cancellationToken);
        if (eligible.IsFailure)
            return Result<Guid>.Failure(eligible.Error!);

        var existing = await subscriptions.GetByAccountAsync(accountResult.Value!, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("This account already has a subscription.");

        var id = SubscriptionId.From(Guid.NewGuid());
        var trial = TrialPeriod.Of(clock.Today, command.TrialDays);

        var subscriptionResult = Subscription.CreateTrial(id, accountResult.Value!, trial);
        if (subscriptionResult.IsFailure)
            return Result<Guid>.Failure(subscriptionResult.Error!);

        await subscriptions.AddAsync(subscriptionResult.Value!, cancellationToken);

        return Result<Guid>.Success(id.Value);
    }
}
