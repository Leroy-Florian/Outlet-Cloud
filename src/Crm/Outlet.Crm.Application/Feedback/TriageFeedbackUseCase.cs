using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Feedback;

public sealed record TriageFeedbackCommand(FeedbackId FeedbackId);

/// <summary>Acknowledges a New feedback and queues it for action.</summary>
public sealed class TriageFeedbackUseCase(IFeedbackRepository feedbackItems) : IUseCase<TriageFeedbackCommand>
{
    public async Task<Result> HandleAsync(TriageFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        var feedback = await feedbackItems.GetByIdAsync(command.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Failure(FeedbackErrors.NotFound(command.FeedbackId));
        }

        var triaged = feedback.Triage();
        if (triaged.IsFailure)
        {
            return triaged;
        }

        await feedbackItems.UpdateAsync(feedback, cancellationToken);
        return Result.Success();
    }
}
