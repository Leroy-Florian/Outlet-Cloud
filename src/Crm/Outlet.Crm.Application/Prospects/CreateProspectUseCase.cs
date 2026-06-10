using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Prospects;

public sealed record CreateProspectCommand(Guid ProductId, Guid? OrganizationId, string Name, string Email, string? Company);

/// <summary>Creates a <see cref="Prospect"/> attached to a product (and optionally an organization).</summary>
public sealed class CreateProspectUseCase(
    IProspectRepository prospects,
    IProductRepository products,
    IOrganizationRepository organizations,
    ICurrentDateTimeProvider clock)
    : IUseCase<CreateProspectCommand, ProspectId>
{
    public async Task<Result<ProspectId>> HandleAsync(CreateProspectCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<ProspectId>(ProductErrors.NotFound(productId));
        }

        OrganizationId? organizationId = null;
        if (command.OrganizationId is { } rawOrganizationId)
        {
            organizationId = new OrganizationId(rawOrganizationId);
            if (await organizations.GetByIdAsync(organizationId.Value, cancellationToken) is null)
            {
                return Result.Failure<ProspectId>(OrganizationErrors.NotFound(organizationId.Value));
            }
        }

        var email = Email.Create(command.Email);
        if (email.IsFailure)
        {
            return Result.Failure<ProspectId>(email.Error!);
        }

        var prospect = Prospect.Create(productId, organizationId, command.Name, email.Value!, command.Company, clock.UtcNow);
        if (prospect.IsFailure)
        {
            return Result.Failure<ProspectId>(prospect.Error!);
        }

        await prospects.AddAsync(prospect.Value!, cancellationToken);

        return Result.Success(prospect.Value!.Id);
    }
}
