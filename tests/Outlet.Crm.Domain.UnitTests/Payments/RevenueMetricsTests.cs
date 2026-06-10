using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Payments;

public sealed class RevenueMetricsTests
{
    private static readonly DateOnly Today = new(2026, 6, 10);

    private static Payment SettledPayment(
        ProductId productId,
        decimal amount,
        DateTime createdAt,
        bool isRecurring = false,
        string currency = "EUR")
    {
        var payment = Payment.Create(
            productId, null, Money.Create(amount, currency).Value!, "stripe", "ref", createdAt, isRecurring).Value!;
        payment.Settle();
        return payment;
    }

    [Fact]
    public void Should_ProduceOnePointPerMonth_When_WindowIsRequested()
    {
        var report = RevenueMetrics.Compute([], Today, 12);

        report.Months.Should().Be(12);
        report.Series.Should().HaveCount(12);
        report.Series[0].Year.Should().Be(2025);
        report.Series[0].Month.Should().Be(7);
        report.Series[^1].Year.Should().Be(2026);
        report.Series[^1].Month.Should().Be(6);
        report.PrimaryCurrency.Should().Be("EUR");
    }

    [Fact]
    public void Should_BucketRevenueByMonth_When_PaymentsAreSettled()
    {
        var product = ProductId.New();
        List<Payment> payments =
        [
            SettledPayment(product, 100m, new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc)),
            SettledPayment(product, 50m, new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc)),
            SettledPayment(product, 30m, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
        ];

        var report = RevenueMetrics.Compute(payments, Today, 3);

        report.Series[0].Total.Should().Be(0m);
        report.Series[1].Total.Should().Be(150m);
        report.Series[2].Total.Should().Be(30m);
        report.Series[2].Cumulative.Should().Be(180m);
    }

    [Fact]
    public void Should_IgnorePayment_When_NotSettled()
    {
        var pending = Payment.Create(
            ProductId.New(), null, Money.Create(99m, "EUR").Value!, "stripe", "ref",
            new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)).Value!;

        var report = RevenueMetrics.Compute([pending], Today, 2);

        report.Series[^1].Total.Should().Be(0m);
        report.CurrencyTotals.Should().BeEmpty();
    }

    [Fact]
    public void Should_IgnorePayment_When_OlderThanTheWindow()
    {
        var old = SettledPayment(ProductId.New(), 500m, new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));

        var report = RevenueMetrics.Compute([old], Today, 3);

        report.Series.Sum(p => p.Total).Should().Be(0m);
        report.CurrencyTotals.Should().BeEmpty();
    }

    [Fact]
    public void Should_IncludePayment_When_OnTheFirstDayOfTheWindow()
    {
        var payment = SettledPayment(ProductId.New(), 500m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        var report = RevenueMetrics.Compute([payment], Today, 3);

        report.Series[0].Total.Should().Be(500m);
    }

    [Fact]
    public void Should_ComputeMrrFromCurrentMonthRecurringRevenue_When_PaymentsAreFlagged()
    {
        var product = ProductId.New();
        List<Payment> payments =
        [
            SettledPayment(product, 29m, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
            SettledPayment(product, 29m, new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
            SettledPayment(product, 500m, new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)),
        ];

        var report = RevenueMetrics.Compute(payments, Today, 2);

        report.MonthlyRecurringRevenue.Should().Be(58m);
        report.Series[^1].Recurring.Should().Be(58m);
        report.Series[^1].Total.Should().Be(558m);
    }

    [Fact]
    public void Should_CountChurnMonths_When_RecurringRevenueDecreases()
    {
        var product = ProductId.New();
        List<Payment> payments =
        [
            SettledPayment(product, 100m, new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
            SettledPayment(product, 50m, new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
            SettledPayment(product, 80m, new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
            SettledPayment(product, 80m, new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
        ];

        var report = RevenueMetrics.Compute(payments, Today, 4);

        report.ChurnMonths.Should().Be(1);
    }

    [Fact]
    public void Should_NotCountChurn_When_RecurringRevenueIsStable()
    {
        var product = ProductId.New();
        List<Payment> payments =
        [
            SettledPayment(product, 80m, new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
            SettledPayment(product, 80m, new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), isRecurring: true),
        ];

        RevenueMetrics.Compute(payments, Today, 2).ChurnMonths.Should().Be(0);
    }

    [Fact]
    public void Should_KeepNonEuroAmountsOutOfTheSeries_When_CurrenciesDiffer()
    {
        var product = ProductId.New();
        List<Payment> payments =
        [
            SettledPayment(product, 100m, new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)),
            SettledPayment(product, 70m, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc), currency: "USD"),
        ];

        var report = RevenueMetrics.Compute(payments, Today, 1);

        report.Series[^1].Total.Should().Be(100m);
        report.CurrencyTotals.Should().BeEquivalentTo(
        [
            new CurrencyTotal("EUR", 100m),
            new CurrencyTotal("USD", 70m),
        ]);
    }

    [Fact]
    public void Should_BreakRevenueDownPerProduct_When_SeveralProductsEarn()
    {
        var productA = ProductId.New();
        var productB = ProductId.New();
        List<Payment> payments =
        [
            SettledPayment(productA, 100m, new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)),
            SettledPayment(productA, 20m, new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc)),
            SettledPayment(productB, 40m, new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc)),
        ];

        var report = RevenueMetrics.Compute(payments, Today, 1);

        report.Series[^1].ByProduct.Should().BeEquivalentTo(
        [
            new MonthlyProductRevenue(productA.Value, 120m),
            new MonthlyProductRevenue(productB.Value, 40m),
        ]);
    }
}
