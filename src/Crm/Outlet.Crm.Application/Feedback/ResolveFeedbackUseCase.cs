using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Feedback;

public sealed record ResolveFeedbackCommand(FeedbackId FeedbackId);

/// <summary>Closes a New or Triaged feedback as resolved.</summary>
public sealed class ResolveFeedbackUseCase(IFeedbackRepository feedbackItems) : IUseCase<ResolveFeedbackCommand>
{
    public async Task<Result> HandleAsync(ResolveFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        var feedback = await feedbackItems.GetByIdAsync(command.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Failure(FeedbackErrors.NotFound(command.FeedbackId));
        }

        var resolved = feedback.Resolve();
        if (resolved.IsFailure)
        {
            return resolved;
        }

        await feedbackItems.UpdateAsync(feedback, cancellationToken);
        return Result.Success();
    }
}
