namespace Outlet.Crm.Domain.Feedback;

public readonly record struct FeedbackId(Guid Value)
{
    public static FeedbackId New() => new(Guid.NewGuid());
}
