using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Payments;

public sealed class RecordPaymentUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _repository = new();
    private readonly FakeProductRepository _products = new();
    private readonly FakeOrganizationRepository _organizations = new();
    private readonly Product _product = Product.Create("FluxPDF", null, Now).Value!;

    public RecordPaymentUseCaseTests() => _products.Items.Add(_product);

    private RecordPaymentUseCase UseCase => new(_repository, _products, _organizations, new FixedClock(Now));

    [Fact]
    public async Task Should_PersistPendingPayment_When_CommandIsValid()
    {
        var result = await UseCase.HandleAsync(
            new RecordPaymentCommand(_product.Id.Value, null, 49.99m, "eur", "stripe", "pi_123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var payment = _repository.Items.Should().ContainSingle().Subject;
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ProductId.Should().Be(_product.Id);
        payment.OrganizationId.Should().BeNull();
        payment.Amount.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task Should_LinkOrganization_When_ItExists()
    {
        var organization = Organization.Create("Acme", null, Now).Value!;
        _organizations.Items.Add(organization);

        var result = await UseCase.HandleAsync(
            new RecordPaymentCommand(_product.Id.Value, organization.Id.Value, 10m, "EUR", "stripe", "pi_2"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Items.Should().ContainSingle().Which.OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationDoesNotExist()
    {
        var result = await UseCase.HandleAsync(
            new RecordPaymentCommand(_product.Id.Value, Guid.NewGuid(), 10m, "EUR", "stripe", "pi_3"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Organization.NotFound:");
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await UseCase.HandleAsync(
            new RecordPaymentCommand(Guid.NewGuid(), null, 10m, "EUR", "stripe", "pi_1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_Fail_When_CurrencyIsInvalid()
    {
        var result = await UseCase.HandleAsync(
            new RecordPaymentCommand(_product.Id.Value, null, 10m, "EURO", "stripe", "pi_123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Money.InvalidCurrency:");
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_SettlePayment_When_ItExists()
    {
        await UseCase.HandleAsync(
            new RecordPaymentCommand(_product.Id.Value, null, 10m, "EUR", "stripe", "pi_1"), CancellationToken.None);
        var settle = new SettlePaymentUseCase(_repository);

        var result = await settle.HandleAsync(new SettlePaymentCommand(_repository.Items[0].Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Items[0].Status.Should().Be(PaymentStatus.Settled);
        _repository.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_SettlingUnknownPayment()
    {
        var settle = new SettlePaymentUseCase(_repository);

        var result = await settle.HandleAsync(new SettlePaymentCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Payment.NotFound:");
    }
}
