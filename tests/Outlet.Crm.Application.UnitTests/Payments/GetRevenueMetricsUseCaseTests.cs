using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Payments;

public sealed class GetRevenueMetricsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _payments = new();

    private GetRevenueMetricsUseCase UseCase => new(_payments, new FixedClock(Now));

    [Fact]
    public async Task Should_DefaultToTwelveMonths_When_NoWindowIsGiven()
    {
        var result = await UseCase.HandleAsync(new GetRevenueMetricsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Months.Should().Be(12);
        result.Value.Series.Should().HaveCount(12);
    }

    [Fact]
    public async Task Should_ClampMonths_When_RequestIsOutOfRange()
    {
        (await UseCase.HandleAsync(new GetRevenueMetricsQuery(0), CancellationToken.None))
            .Value!.Months.Should().Be(1);
        (await UseCase.HandleAsync(new GetRevenueMetricsQuery(100), CancellationToken.None))
            .Value!.Months.Should().Be(60);
    }

    [Fact]
    public async Task Should_ComputeFromSettledPayments_When_TheyExist()
    {
        var payment = Payment.Create(
            ProductId.New(), null, Money.Create(49m, "EUR").Value!, "stripe", "pi_1", Now, isRecurring: true).Value!;
        payment.Settle();
        _payments.Items.Add(payment);

        var result = await UseCase.HandleAsync(new GetRevenueMetricsQuery(3), CancellationToken.None);

        var report = result.Value!;
        report.Series[^1].Total.Should().Be(49m);
        report.MonthlyRecurringRevenue.Should().Be(49m);
        report.CurrencyTotals.Should().ContainSingle().Which.Should().Be(new CurrencyTotal("EUR", 49m));
    }
}
