using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Prospects;

public sealed record CreateProspectCommand(
    Guid ProductId,
    Guid? OrganizationId,
    string Name,
    string Email,
    string? Company,
    decimal? EstimatedValue = null,
    string? EstimatedValueCurrency = null);

/// <summary>Maps the optional (amount, currency) pair of a command onto the <see cref="Money"/> VO (currency defaults to EUR).</summary>
internal static class ProspectEstimatedValue
{
    private const string DefaultCurrency = "EUR";

    internal static Result<Money?> Resolve(decimal? amount, string? currency)
    {
        if (amount is not { } value)
        {
            return Result.Success<Money?>(null);
        }

        var money = Money.Create(value, currency ?? DefaultCurrency);
        return money.IsFailure ? Result.Failure<Money?>(money.Error!) : Result.Success<Money?>(money.Value!);
    }
}

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

        var estimatedValue = ProspectEstimatedValue.Resolve(command.EstimatedValue, command.EstimatedValueCurrency);
        if (estimatedValue.IsFailure)
        {
            return Result.Failure<ProspectId>(estimatedValue.Error!);
        }

        var prospect = Prospect.Create(
            productId, organizationId, command.Name, email.Value!, command.Company, estimatedValue.Value, clock.UtcNow);
        if (prospect.IsFailure)
        {
            return Result.Failure<ProspectId>(prospect.Error!);
        }

        await prospects.AddAsync(prospect.Value!, cancellationToken);

        return Result.Success(prospect.Value!.Id);
    }
}
