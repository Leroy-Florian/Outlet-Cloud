using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Organizations;

/// <summary>Command: open or close anonymous read access to an organization's registry.</summary>
public sealed record ChangeRegistryVisibilityCommand(
    Guid OrganizationId,
    Guid ActorUserId,
    RegistryVisibility Visibility);

/// <summary>
/// Changes an organization's registry visibility. Authorization is decided here,
/// server-side: only an Owner or Admin member may flip the switch.
/// </summary>
public sealed class ChangeRegistryVisibilityUseCase(IOrganizationRepository organizations)
    : IUseCase<ChangeRegistryVisibilityCommand>
{
    public async Task<Result> HandleAsync(ChangeRegistryVisibilityCommand command, CancellationToken cancellationToken = default)
    {
        var idResult = Guard.TryBuild(() => OrganizationId.From(command.OrganizationId), "Organization id is invalid.");
        if (idResult.IsFailure)
            return Result.Failure(idResult.Error!);

        var actorResult = Guard.TryBuild(() => MemberUserId.From(command.ActorUserId), "User id is invalid.");
        if (actorResult.IsFailure)
            return Result.Failure(actorResult.Error!);

        var organization = await organizations.GetByIdAsync(idResult.Value!, cancellationToken);
        if (organization is null)
            return Result.Failure($"Organization '{idResult.Value}' was not found.");

        var actor = organization.Memberships.FirstOrDefault(m => m.Id == actorResult.Value!);
        if (actor is null || actor.Role is not (OrganizationRole.Owner or OrganizationRole.Admin))
            return Result.Failure("Only owners and admins can change registry visibility.");

        var change = organization.ChangeRegistryVisibility(command.Visibility);
        if (change.IsFailure)
            return change;

        await organizations.UpdateAsync(organization, cancellationToken);

        return Result.Success();
    }
}
