using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Feedback;

public sealed record SubmitFeedbackCommand(
    Guid ProductId,
    FeedbackCategory Category,
    string Message,
    string? ReporterEmail,
    string? SourceApp);

/// <summary>
/// Public ingestion: a user's app submits feedback that lands as a New inbox
/// item attached to an existing product. The clock stamps the reception time.
/// </summary>
public sealed class SubmitFeedbackUseCase(
    IFeedbackRepository feedbackItems,
    IProductRepository products,
    ICurrentDateTimeProvider clock)
    : IUseCase<SubmitFeedbackCommand, FeedbackId>
{
    public async Task<Result<FeedbackId>> HandleAsync(SubmitFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(command.ProductId);
        if (await products.GetByIdAsync(productId, cancellationToken) is null)
        {
            return Result.Failure<FeedbackId>(ProductErrors.NotFound(productId));
        }

        Email? reporterEmail = null;
        if (!string.IsNullOrWhiteSpace(command.ReporterEmail))
        {
            var email = Email.Create(command.ReporterEmail);
            if (email.IsFailure)
            {
                return Result.Failure<FeedbackId>(email.Error!);
            }

            reporterEmail = email.Value!;
        }

        var feedback = Domain.Feedback.Feedback.Create(
            productId, command.Category, command.Message, reporterEmail, command.SourceApp, clock.UtcNow);
        if (feedback.IsFailure)
        {
            return Result.Failure<FeedbackId>(feedback.Error!);
        }

        await feedbackItems.AddAsync(feedback.Value!, cancellationToken);

        return Result.Success(feedback.Value!.Id);
    }
}
