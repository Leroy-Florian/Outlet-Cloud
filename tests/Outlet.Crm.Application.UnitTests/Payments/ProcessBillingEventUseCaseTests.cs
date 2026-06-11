using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Payments;

public sealed class ProcessBillingEventUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _payments = new();
    private readonly FakeProductRepository _products = new();
    private readonly Product _product = Product.Create("FluxPDF", null, Now).Value!;

    public ProcessBillingEventUseCaseTests() => _products.Items.Add(_product);

    private ProcessBillingEventUseCase UseCase => new(_payments, _products, new FixedClock(Now));

    private ProcessBillingEventCommand Command(
        string reference = "evt_1",
        string? status = null,
        decimal amount = 49m,
        string currency = "EUR",
        bool? isRecurring = null,
        Guid? productId = null) =>
        new(reference, productId ?? _product.Id.Value, amount, currency, isRecurring, status);

    [Fact]
    public async Task Should_RecordSettledPayment_When_StatusIsPaid()
    {
        var result = await UseCase.HandleAsync(Command(status: "paid"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(BillingEventOutcome.Recorded);
        var payment = _payments.Items.Should().ContainSingle().Subject;
        payment.Status.Should().Be(PaymentStatus.Settled);
        payment.ExternalReference.Should().Be("evt_1");
        payment.Source.Should().Be("billing-webhook");
        payment.ProductId.Should().Be(_product.Id);
        payment.Amount.Amount.Should().Be(49m);
        payment.Amount.Currency.Should().Be("EUR");
        payment.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_DefaultToPaid_When_StatusIsMissing()
    {
        var result = await UseCase.HandleAsync(Command(status: null), CancellationToken.None);

        result.Value.Should().Be(BillingEventOutcome.Recorded);
        _payments.Items.Should().ContainSingle().Which.Status.Should().Be(PaymentStatus.Settled);
    }

    [Fact]
    public async Task Should_RecordPendingPayment_When_StatusIsPending()
    {
        var result = await UseCase.HandleAsync(Command(status: "pending"), CancellationToken.None);

        result.Value.Should().Be(BillingEventOutcome.Recorded);
        _payments.Items.Should().ContainSingle().Which.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Should_FlagRecurring_When_EventSaysSo()
    {
        await UseCase.HandleAsync(Command(isRecurring: true), CancellationToken.None);

        _payments.Items.Should().ContainSingle().Which.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnAlreadyProcessed_When_SameReferenceArrivesTwice()
    {
        await UseCase.HandleAsync(Command(), CancellationToken.None);

        var replay = await UseCase.HandleAsync(Command(), CancellationToken.None);

        replay.IsSuccess.Should().BeTrue();
        replay.Value.Should().Be(BillingEventOutcome.AlreadyProcessed);
        _payments.Items.Should().ContainSingle();
        _payments.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_MatchExistingPayment_When_ReferenceHasSurroundingWhitespace()
    {
        await UseCase.HandleAsync(Command(), CancellationToken.None);

        var replay = await UseCase.HandleAsync(Command(reference: "  evt_1  "), CancellationToken.None);

        replay.Value.Should().Be(BillingEventOutcome.AlreadyProcessed);
        _payments.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_RefundSettledPayment_When_StatusIsRefunded()
    {
        await UseCase.HandleAsync(Command(status: "paid"), CancellationToken.None);

        var result = await UseCase.HandleAsync(Command(status: "refunded"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(BillingEventOutcome.Refunded);
        _payments.Items.Should().ContainSingle().Which.Status.Should().Be(PaymentStatus.Refunded);
        _payments.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnAlreadyProcessed_When_RefundArrivesTwice()
    {
        await UseCase.HandleAsync(Command(status: "paid"), CancellationToken.None);
        await UseCase.HandleAsync(Command(status: "refunded"), CancellationToken.None);

        var replay = await UseCase.HandleAsync(Command(status: "refunded"), CancellationToken.None);

        replay.IsSuccess.Should().BeTrue();
        replay.Value.Should().Be(BillingEventOutcome.AlreadyProcessed);
        _payments.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_RefundTargetsUnknownReference()
    {
        var result = await UseCase.HandleAsync(Command(status: "refunded"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotFoundByReference("evt_1"));
    }

    [Fact]
    public async Task Should_Fail_When_RefundTargetsPendingPayment()
    {
        await UseCase.HandleAsync(Command(status: "pending"), CancellationToken.None);

        var result = await UseCase.HandleAsync(Command(status: "refunded"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotSettled);
        _payments.Items.Should().ContainSingle().Which.Status.Should().Be(PaymentStatus.Pending);
        _payments.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_ExternalReferenceIsMissing()
    {
        var result = await UseCase.HandleAsync(Command(reference: "  "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.ExternalReferenceRequired);
        _payments.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_StatusIsUnknown()
    {
        var result = await UseCase.HandleAsync(Command(status: "disputed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.UnknownBillingStatus("disputed"));
        _payments.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_AcceptStatus_When_CasingDiffers()
    {
        var result = await UseCase.HandleAsync(Command(status: "PAID"), CancellationToken.None);

        result.Value.Should().Be(BillingEventOutcome.Recorded);
        _payments.Items.Should().ContainSingle().Which.Status.Should().Be(PaymentStatus.Settled);
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var unknownProduct = Guid.NewGuid();

        var result = await UseCase.HandleAsync(Command(productId: unknownProduct), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound(new ProductId(unknownProduct)));
        _payments.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_AmountIsInvalid()
    {
        var result = await UseCase.HandleAsync(Command(amount: -1m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _payments.Items.Should().BeEmpty();
    }
}
