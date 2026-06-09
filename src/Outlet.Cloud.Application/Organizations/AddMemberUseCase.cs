using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Organizations;

/// <summary>Command: add a user to an organization with a role.</summary>
public sealed record AddMemberCommand(Guid OrganizationId, Guid UserId, OrganizationRole Role);

/// <summary>Adds a member to an organization, delegating the invariants to the aggregate.</summary>
public sealed class AddMemberUseCase(IOrganizationRepository organizations)
    : IUseCase<AddMemberCommand>
{
    public async Task<Result> HandleAsync(AddMemberCommand command, CancellationToken cancellationToken = default)
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

        var add = organization.AddMember(userResult.Value!, command.Role);
        if (add.IsFailure)
            return add;

        await organizations.UpdateAsync(organization, cancellationToken);

        return Result.Success();
    }
}
