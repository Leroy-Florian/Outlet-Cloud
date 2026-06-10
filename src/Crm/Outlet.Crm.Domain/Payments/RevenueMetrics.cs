namespace Outlet.Crm.Domain.Payments;

public sealed record MonthlyProductRevenue(Guid ProductId, decimal Amount);

public sealed record MonthlyRevenuePoint(
    int Year,
    int Month,
    decimal Total,
    decimal Recurring,
    decimal Cumulative,
    IReadOnlyList<MonthlyProductRevenue> ByProduct);

public sealed record CurrencyTotal(string Currency, decimal Total);

public sealed record RevenueMetricsReport(
    string PrimaryCurrency,
    int Months,
    IReadOnlyList<MonthlyRevenuePoint> Series,
    decimal MonthlyRecurringRevenue,
    int ChurnMonths,
    IReadOnlyList<CurrencyTotal> CurrencyTotals);

/// <summary>
/// Monthly revenue series computed from SETTLED payments only, attributed to the
/// month of <see cref="Payment.CreatedAt"/>. Currency handling is deliberately
/// simple v1: amounts are never converted, so the per-month series and cumulative
/// only sum the primary currency (EUR); other currencies are reported in
/// <see cref="RevenueMetricsReport.CurrencyTotals"/>. Approximations, documented:
/// MRR ≈ the current month's revenue from payments flagged recurring; churn proxy
/// ≈ the number of months whose recurring revenue is lower than the previous
/// month's (a real churn rate needs per-customer subscriptions we don't track yet).
/// </summary>
public static class RevenueMetrics
{
    public const string PrimaryCurrency = "EUR";

    public static RevenueMetricsReport Compute(IEnumerable<Payment> payments, DateOnly today, int months)
    {
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        List<Payment> settled = [.. payments
            .Where(p => p.Status == PaymentStatus.Settled)
            .Where(p => DateOnly.FromDateTime(p.CreatedAt) >= firstMonth)];

        List<CurrencyTotal> currencyTotals = [.. settled
            .GroupBy(p => p.Amount.Currency)
            .Select(g => new CurrencyTotal(g.Key, g.Sum(p => p.Amount.Amount)))
            .OrderBy(c => c.Currency, StringComparer.Ordinal)];

        List<Payment> primary = [.. settled.Where(p => p.Amount.Currency == PrimaryCurrency)];

        List<MonthlyRevenuePoint> series = [];
        var cumulative = 0m;
        var churnMonths = 0;
        decimal? previousRecurring = null;

        for (var i = 0; i < months; i++)
        {
            var month = firstMonth.AddMonths(i);
            List<Payment> inMonth = [.. primary
                .Where(p => p.CreatedAt.Year == month.Year && p.CreatedAt.Month == month.Month)];

            var total = inMonth.Sum(p => p.Amount.Amount);
            var recurring = inMonth.Where(p => p.IsRecurring).Sum(p => p.Amount.Amount);
            cumulative += total;

            if (previousRecurring is { } previous && recurring < previous)
            {
                churnMonths++;
            }

            previousRecurring = recurring;

            List<MonthlyProductRevenue> byProduct = [.. inMonth
                .GroupBy(p => p.ProductId.Value)
                .Select(g => new MonthlyProductRevenue(g.Key, g.Sum(p => p.Amount.Amount)))
                .OrderBy(p => p.ProductId)];

            series.Add(new MonthlyRevenuePoint(month.Year, month.Month, total, recurring, cumulative, byProduct));
        }

        return new RevenueMetricsReport(
            PrimaryCurrency,
            months,
            series,
            series[^1].Recurring,
            churnMonths,
            currencyTotals);
    }
}
