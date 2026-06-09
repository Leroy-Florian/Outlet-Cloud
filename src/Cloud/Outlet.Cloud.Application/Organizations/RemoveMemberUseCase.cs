using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Organizations;

/// <summary>Command: remove a user from an organization.</summary>
public sealed record RemoveMemberCommand(Guid OrganizationId, Guid UserId);

/// <summary>Removes a member; the aggregate refuses to remove the last owner.</summary>
public sealed class RemoveMemberUseCase(IOrganizationRepository organizations)
    : IUseCase<RemoveMemberCommand>
{
    public async Task<Result> HandleAsync(RemoveMemberCommand command, CancellationToken cancellationToken = default)
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

        var remove = organization.RemoveMember(userResult.Value!);
        if (remove.IsFailure)
            return remove;

        await organizations.UpdateAsync(organization, cancellationToken);

        return Result.Success();
    }
}
