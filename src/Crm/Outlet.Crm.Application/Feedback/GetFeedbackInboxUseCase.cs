using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Feedback;

public sealed record GetFeedbackInboxQuery(Guid? ProductId, FeedbackStatus? Status, FeedbackCategory? Category);

public sealed record FeedbackStatusCounts(int New, int Triaged, int Resolved, int Dismissed)
{
    public int Total => New + Triaged + Resolved + Dismissed;
}

public sealed record FeedbackInbox(
    IReadOnlyList<Domain.Feedback.Feedback> Items,
    FeedbackStatusCounts Counts);

/// <summary>
/// Inbox view: feedback items newest first, optionally filtered by product,
/// status and category. The per-status counts reflect the product and category
/// filters but NOT the status filter, so the inbox tabs stay populated while
/// one status is selected.
/// </summary>
public sealed class GetFeedbackInboxUseCase(
    IFeedbackRepository feedbackItems,
    IProductRepository products)
    : IUseCase<GetFeedbackInboxQuery, FeedbackInbox>
{
    public async Task<Result<FeedbackInbox>> HandleAsync(GetFeedbackInboxQuery command, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Feedback.Feedback> all;
        if (command.ProductId is { } rawProductId)
        {
            var productId = new ProductId(rawProductId);
            if (await products.GetByIdAsync(productId, cancellationToken) is null)
            {
                return Result.Failure<FeedbackInbox>(ProductErrors.NotFound(productId));
            }

            all = await feedbackItems.ListByProductAsync(productId, cancellationToken);
        }
        else
        {
            all = await feedbackItems.ListAsync(cancellationToken);
        }

        List<Domain.Feedback.Feedback> scoped = [.. all
            .Where(f => command.Category is null || f.Category == command.Category)];

        var counts = new FeedbackStatusCounts(
            scoped.Count(f => f.Status == FeedbackStatus.New),
            scoped.Count(f => f.Status == FeedbackStatus.Triaged),
            scoped.Count(f => f.Status == FeedbackStatus.Resolved),
            scoped.Count(f => f.Status == FeedbackStatus.Dismissed));

        List<Domain.Feedback.Feedback> items = [.. scoped
            .Where(f => command.Status is null || f.Status == command.Status)
            .OrderByDescending(f => f.ReceivedAt)];

        return Result.Success(new FeedbackInbox(items, counts));
    }
}
