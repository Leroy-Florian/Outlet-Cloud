using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Payments;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Payments;

public sealed record GetRevenueMetricsQuery(int? Months = null);

/// <summary>
/// Monthly revenue metrics over the trailing window (default 12 months, clamped
/// to [1, 60]) computed from settled payments; the math and its documented
/// approximations (EUR-only series, MRR, churn proxy) live in <see cref="RevenueMetrics"/>.
/// </summary>
public sealed class GetRevenueMetricsUseCase(
    IPaymentRepository payments,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetRevenueMetricsQuery, RevenueMetricsReport>
{
    private const int DefaultMonths = 12;
    private const int MaxMonths = 60;

    public async Task<Result<RevenueMetricsReport>> HandleAsync(
        GetRevenueMetricsQuery command,
        CancellationToken cancellationToken = default)
    {
        var months = Math.Clamp(command.Months ?? DefaultMonths, 1, MaxMonths);
        var all = await payments.ListAsync(cancellationToken);

        return Result.Success(RevenueMetrics.Compute(all, clock.Today, months));
    }
}
