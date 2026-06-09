using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Organizations;

/// <summary>Command: change an existing member's role.</summary>
public sealed record ChangeMemberRoleCommand(Guid OrganizationId, Guid UserId, OrganizationRole Role);

/// <summary>Changes a member's role; the aggregate refuses to demote the last owner.</summary>
public sealed class ChangeMemberRoleUseCase(IOrganizationRepository organizations)
    : IUseCase<ChangeMemberRoleCommand>
{
    public async Task<Result> HandleAsync(ChangeMemberRoleCommand command, CancellationToken cancellationToken = default)
    {
        var idResult = Guard.TryBuild(() => OrganizationId.From(command.OrganizationId), "Organization id is invalid.");
        if (idResult.IsFailure)
            return Result.Failure(idResult.Error!);

        var userResult = Guard.TryBuild(() => MemberUserId.From(command.UserId), "User id is invalid.");
        if (userResult.IsFailure)
            return Result.Failure(userResult.Error!);

        var organization = await organizations.GetByIdAsync(idResult.Value!, cancellationToken);
        if (organization is null)
            return Result.Failure($"Organization '{idResult.Value}' was not found.");

        var change = organization.ChangeRole(userResult.Value!, command.Role);
        if (change.IsFailure)
            return change;

        await organizations.UpdateAsync(organization, cancellationToken);

        return Result.Success();
    }
}
