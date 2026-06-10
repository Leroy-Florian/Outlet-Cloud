using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Feedback;

public sealed record DismissFeedbackCommand(FeedbackId FeedbackId);

/// <summary>Closes a New or Triaged feedback without action.</summary>
public sealed class DismissFeedbackUseCase(IFeedbackRepository feedbackItems) : IUseCase<DismissFeedbackCommand>
{
    public async Task<Result> HandleAsync(DismissFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        var feedback = await feedbackItems.GetByIdAsync(command.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Failure(FeedbackErrors.NotFound(command.FeedbackId));
        }

        var dismissed = feedback.Dismiss();
        if (dismissed.IsFailure)
        {
            return dismissed;
        }

        await feedbackItems.UpdateAsync(feedback, cancellationToken);
        return Result.Success();
    }
}
