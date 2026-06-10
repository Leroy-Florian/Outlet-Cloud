using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Payments;

public sealed class PaymentTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Payment CreatePayment() =>
        Payment.Create(ProductId.New(), null, Money.Create(49.99m, "eur").Value!, "stripe", "pi_123", Now).Value!;

    [Fact]
    public void Should_StartPending_When_Created()
    {
        CreatePayment().Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Should_Fail_When_SourceIsBlank()
    {
        var result = Payment.Create(ProductId.New(), null, Money.Create(1m, "EUR").Value!, " ", "ref", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.SourceRequired);
    }

    [Fact]
    public void Should_Settle_When_Pending()
    {
        var payment = CreatePayment();

        var result = payment.Settle();

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Settled);
    }

    [Fact]
    public void Should_Fail_When_SettlingTwice()
    {
        var payment = CreatePayment();
        payment.Settle();

        var result = payment.Settle();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotPending);
    }

    [Fact]
    public void Should_Refund_When_Settled()
    {
        var payment = CreatePayment();
        payment.Settle();

        var result = payment.Refund();

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Should_Fail_When_RefundingAPendingPayment()
    {
        var result = CreatePayment().Refund();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotSettled);
    }

    [Fact]
    public void Should_MarkFailed_When_FailingAPendingPayment()
    {
        var payment = CreatePayment();

        var result = payment.Fail();

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void Should_Fail_When_FailingANonPendingPayment()
    {
        var payment = CreatePayment();
        payment.Settle();

        var result = payment.Fail();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotPending);
        payment.Status.Should().Be(PaymentStatus.Settled);
    }

    [Fact]
    public void Should_AcceptZeroAmount_When_CreatingMoney()
    {
        var money = Money.Create(0m, "EUR");

        money.IsSuccess.Should().BeTrue();
        money.Value!.Amount.Should().Be(0m);
    }

    [Fact]
    public void Should_NormalizeCurrency_When_CreatingMoney()
    {
        var money = Money.Create(10m, " eur ");

        money.IsSuccess.Should().BeTrue();
        money.Value!.Currency.Should().Be("EUR");
    }

    [Theory]
    [InlineData(-1, "EUR", "Money.Negative:")]
    [InlineData(1, "EU", "Money.InvalidCurrency:")]
    public void Should_Fail_When_MoneyIsInvalid(decimal amount, string currency, string expectedCodePrefix)
    {
        var result = Money.Create(amount, currency);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith(expectedCodePrefix);
    }
}
