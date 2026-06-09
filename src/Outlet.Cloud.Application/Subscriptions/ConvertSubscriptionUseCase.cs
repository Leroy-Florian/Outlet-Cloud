using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Subscriptions;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Subscriptions;

/// <summary>Command: convert an organization's trial to a paying Pro plan.</summary>
public sealed record ConvertSubscriptionCommand(Guid OrganizationId);

/// <summary>
/// Converts a trialing subscription to Active. The actual billing handshake (Stripe…) plugs in
/// upstream of this use case; here we only flip the domain state once payment is secured.
/// </summary>
public sealed class ConvertSubscriptionUseCase(ISubscriptionRepository subscriptions)
    : IUseCase<ConvertSubscriptionCommand>
{
    public async Task<Result> HandleAsync(ConvertSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var orgResult = Guard.TryBuild(() => OrganizationId.From(command.OrganizationId), "Organization id is invalid.");
        if (orgResult.IsFailure)
            return Result.Failure(orgResult.Error!);

        var subscription = await subscriptions.GetByOrganizationAsync(orgResult.Value!, cancellationToken);
        if (subscription is null)
            return Result.Failure("This organization has no subscription to convert.");

        var conversion = subscription.Convert(PlanTier.Pro);
        if (conversion.IsFailure)
            return conversion;

        await subscriptions.UpdateAsync(subscription, cancellationToken);

        return Result.Success();
    }
}
