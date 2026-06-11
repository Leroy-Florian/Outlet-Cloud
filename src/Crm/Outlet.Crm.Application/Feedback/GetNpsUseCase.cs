using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Feedback;

public sealed record GetNpsQuery(Guid? ProductId, int? Days);

/// <summary>
/// Standard NPS over the scored feedback of the window: promoters (9-10) %
/// minus detractors (0-6) %. <see cref="Score"/> is null when nothing is scored.
/// </summary>
public sealed record NpsReport(double? Score, int Promoters, int Passives, int Detractors, int Total, int Days);

public sealed class GetNpsUseCase(
    IFeedbackRepository feedbackItems,
    IProductRepository products,
    ICurrentDateTimeProvider clock)
    : IUseCase<GetNpsQuery, NpsReport>
{
    private const int DefaultWindowDays = 90;

    public async Task<Result<NpsReport>> HandleAsync(GetNpsQuery command, CancellationToken cancellationToken = default)
    {
        var days = command.Days ?? DefaultWindowDays;
        if (days <= 0)
        {
            return Result.Failure<NpsReport>(
                "Nps.InvalidWindow: 'days' must be a strictly positive number of days.");
        }

        IReadOnlyList<Domain.Feedback.Feedback> all;
        if (command.ProductId is { } rawProductId)
        {
            var productId = new ProductId(rawProductId);
            if (await products.GetByIdAsync(productId, cancellationToken) is null)
            {
                return Result.Failure<NpsReport>(ProductErrors.NotFound(productId));
            }

            all = await feedbackItems.ListByProductAsync(productId, cancellationToken);
        }
        else
        {
            all = await feedbackItems.ListAsync(cancellationToken);
        }

        var since = clock.UtcNow.AddDays(-days);
        List<int> scores = [.. all
            .Where(f => f.Score is not null && f.ReceivedAt >= since)
            .Select(f => f.Score!.Value)];

        var promoters = scores.Count(s => s >= 9);
        var detractors = scores.Count(s => s <= 6);
        var passives = scores.Count - promoters - detractors;

        double? nps = scores.Count == 0
            ? null
            : Math.Round((promoters - detractors) * 100.0 / scores.Count, 1);

        return Result.Success(new NpsReport(nps, promoters, passives, detractors, scores.Count, days));
    }
}
