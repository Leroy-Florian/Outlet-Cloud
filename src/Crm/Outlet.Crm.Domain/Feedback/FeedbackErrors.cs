namespace Outlet.Crm.Domain.Feedback;

public static class FeedbackErrors
{
    public const string MessageRequired =
        "Feedback.MessageRequired: A feedback requires a non-empty message.";

    public const string MessageTooLong =
        "Feedback.MessageTooLong: A feedback message cannot exceed 4000 characters.";

    public const string ScoreOutOfRange =
        "Feedback.ScoreOutOfRange: A feedback score must be between 0 and 10.";

    public static string InvalidTransition(FeedbackStatus from, FeedbackStatus to) =>
        $"Feedback.InvalidTransition: A {from} feedback cannot move to {to}.";

    public static string NotFound(FeedbackId id) =>
        $"Feedback.NotFound: Feedback '{id.Value}' was not found.";
}
