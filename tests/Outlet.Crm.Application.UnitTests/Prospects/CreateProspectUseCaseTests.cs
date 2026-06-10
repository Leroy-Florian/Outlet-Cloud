using Outlet.Crm.Application.Prospects;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Application.UnitTests.Prospects;

public sealed class CreateProspectUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProspectRepository _repository = new();
    private readonly FakeProductRepository _products = new();
    private readonly FakeOrganizationRepository _organizations = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public CreateProspectUseCaseTests() => _products.Items.Add(_product);

    private CreateProspectUseCase UseCase => new(_repository, _products, _organizations, new FixedClock(Now));

    [Fact]
    public async Task Should_PersistProspect_When_CommandIsValid()
    {
        var result = await UseCase.HandleAsync(
            new CreateProspectCommand(_product.Id.Value, null, "Ada", "ada@example.com", "AE"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = _repository.Items.Should().ContainSingle().Subject;
        stored.Id.Should().Be(result.Value);
        stored.ProductId.Should().Be(_product.Id);
        stored.OrganizationId.Should().BeNull();
        stored.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_LinkOrganization_When_ItExists()
    {
        var organization = Organization.Create("Acme", null, Now).Value!;
        _organizations.Items.Add(organization);

        var result = await UseCase.HandleAsync(
            new CreateProspectCommand(_product.Id.Value, organization.Id.Value, "Ada", "ada@example.com", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Items.Should().ContainSingle().Which.OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationDoesNotExist()
    {
        var result = await UseCase.HandleAsync(
            new CreateProspectCommand(_product.Id.Value, Guid.NewGuid(), "Ada", "ada@example.com", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Organization.NotFound:");
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await UseCase.HandleAsync(
            new CreateProspectCommand(Guid.NewGuid(), null, "Ada", "ada@example.com", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_EmailIsInvalid()
    {
        var result = await UseCase.HandleAsync(
            new CreateProspectCommand(_product.Id.Value, null, "Ada", "not-an-email", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Email.Invalid:");
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_NameIsBlank()
    {
        var result = await UseCase.HandleAsync(
            new CreateProspectCommand(_product.Id.Value, null, " ", "ada@example.com", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProspectErrors.NameRequired);
    }
}
