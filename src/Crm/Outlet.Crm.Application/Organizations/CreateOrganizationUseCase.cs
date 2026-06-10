using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Organizations;

public sealed record CreateOrganizationCommand(string Name, string? Website);

/// <summary>Creates a customer <see cref="Organization"/> and persists it.</summary>
public sealed class CreateOrganizationUseCase(IOrganizationRepository organizations, ICurrentDateTimeProvider clock)
    : IUseCase<CreateOrganizationCommand, OrganizationId>
{
    public async Task<Result<OrganizationId>> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var organization = Organization.Create(command.Name, command.Website, clock.UtcNow);
        if (organization.IsFailure)
        {
            return Result.Failure<OrganizationId>(organization.Error!);
        }

        await organizations.AddAsync(organization.Value!, cancellationToken);

        return Result.Success(organization.Value!.Id);
    }
}
