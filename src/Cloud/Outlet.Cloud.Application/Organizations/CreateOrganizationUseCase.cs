using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Organizations;

/// <summary>Command: create an organization with a caller-supplied id and a first owner.</summary>
public sealed record CreateOrganizationCommand(Guid OrganizationId, string Slug, string Name, Guid OwnerUserId);

/// <summary>
/// Creates an <see cref="Organization"/>: validates the value objects, enforces
/// slug uniqueness, seeds the first owner, and persists.
/// </summary>
public sealed class CreateOrganizationUseCase(IOrganizationRepository organizations)
    : IUseCase<CreateOrganizationCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var idResult = Guard.TryBuild(() => OrganizationId.From(command.OrganizationId), "Organization id is invalid.");
        if (idResult.IsFailure)
            return Result<Guid>.Failure(idResult.Error!);

        var slugResult = Guard.TryBuild(() => OrganizationSlug.From(command.Slug), $"Slug '{command.Slug}' is invalid.");
        if (slugResult.IsFailure)
            return Result<Guid>.Failure(slugResult.Error!);

        var nameResult = Guard.TryBuild(() => OrganizationName.From(command.Name), "Organization name is invalid.");
        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error!);

        var ownerResult = Guard.TryBuild(() => MemberUserId.From(command.OwnerUserId), "Owner user id is invalid.");
        if (ownerResult.IsFailure)
            return Result<Guid>.Failure(ownerResult.Error!);

        if (await organizations.ExistsWithSlugAsync(slugResult.Value!, cancellationToken))
            return Result<Guid>.Failure($"An organization with slug '{slugResult.Value}' already exists.");

        var organizationResult = Organization.Create(idResult.Value!, slugResult.Value!, nameResult.Value!, ownerResult.Value!);
        if (organizationResult.IsFailure)
            return Result<Guid>.Failure(organizationResult.Error!);

        await organizations.AddAsync(organizationResult.Value!, cancellationToken);

        return Result<Guid>.Success(idResult.Value!.Value);
    }
}
