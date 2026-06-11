using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Feedback;

/// <summary>
/// One piece of feedback sent from a user's app (CLI, website, in-app form…)
/// landing as a triageable inbox item. State machine:
/// New → Triaged → Resolved, with Dismissed reachable from New or Triaged.
/// Resolved and Dismissed are terminal.
/// </summary>
public sealed class Feedback : AggregateRoot<FeedbackId>
{
    private const int MaxMessageLength = 4000;

    private Feedback(
        FeedbackId id,
        ProductId productId,
        FeedbackCategory category,
        string message,
        Email? reporterEmail,
        string? sourceApp,
        int? score,
        DateTime receivedAt)
        : base(id)
    {
        ProductId = productId;
        Category = category;
        Message = message;
        ReporterEmail = reporterEmail;
        SourceApp = sourceApp;
        Score = score;
        ReceivedAt = receivedAt;
        Status = FeedbackStatus.New;
    }

    public ProductId ProductId { get; }

    public FeedbackCategory Category { get; }

    public string Message { get; }

    public Email? ReporterEmail { get; }

    /// <summary>Free-form origin tag supplied by the client, e.g. "outlet-cli@1.2.0".</summary>
    public string? SourceApp { get; }

    /// <summary>Optional 0-10 satisfaction score (NPS scale), independent of the category.</summary>
    public int? Score { get; }

    public FeedbackStatus Status { get; private set; }

    public DateTime ReceivedAt { get; }

    public static Result<Feedback> Create(
        ProductId productId,
        FeedbackCategory category,
        string message,
        Email? reporterEmail,
        string? sourceApp,
        DateTime receivedAt) =>
        Create(productId, category, message, reporterEmail, sourceApp, null, receivedAt);

    public static Result<Feedback> Create(
        ProductId productId,
        FeedbackCategory category,
        string message,
        Email? reporterEmail,
        string? sourceApp,
        int? score,
        DateTime receivedAt)
    {
        if (score is < 0 or > 10)
        {
            return Result.Failure<Feedback>(FeedbackErrors.ScoreOutOfRange);
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Failure<Feedback>(FeedbackErrors.MessageRequired);
        }

        var trimmedMessage = message.Trim();
        if (trimmedMessage.Length > MaxMessageLength)
        {
            return Result.Failure<Feedback>(FeedbackErrors.MessageTooLong);
        }

        var trimmedSource = sourceApp?.Trim();
        var feedback = new Feedback(
            FeedbackId.New(),
            productId,
            category,
            trimmedMessage,
            reporterEmail,
            string.IsNullOrEmpty(trimmedSource) ? null : trimmedSource,
            score,
            receivedAt);

        feedback.RaiseDomainEvent(new FeedbackReceivedEvent(feedback.Id, productId, category));

        return Result.Success(feedback);
    }

    public Result Triage()
    {
        if (Status is not FeedbackStatus.New)
        {
            return Result.Failure(FeedbackErrors.InvalidTransition(Status, FeedbackStatus.Triaged));
        }

        Status = FeedbackStatus.Triaged;
        RaiseDomainEvent(new FeedbackTriagedEvent(Id));
        return Result.Success();
    }

    public Result Resolve()
    {
        if (Status is not (FeedbackStatus.New or FeedbackStatus.Triaged))
        {
            return Result.Failure(FeedbackErrors.InvalidTransition(Status, FeedbackStatus.Resolved));
        }

        Status = FeedbackStatus.Resolved;
        RaiseDomainEvent(new FeedbackResolvedEvent(Id));
        return Result.Success();
    }

    public Result Dismiss()
    {
        if (Status is not (FeedbackStatus.New or FeedbackStatus.Triaged))
        {
            return Result.Failure(FeedbackErrors.InvalidTransition(Status, FeedbackStatus.Dismissed));
        }

        Status = FeedbackStatus.Dismissed;
        RaiseDomainEvent(new FeedbackDismissedEvent(Id));
        return Result.Success();
    }
}
