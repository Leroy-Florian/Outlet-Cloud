using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Feedback;

/// <summary>Raised when a user-submitted feedback lands in the inbox.</summary>
public sealed record FeedbackReceivedEvent(FeedbackId FeedbackId, ProductId ProductId, FeedbackCategory Category) : DomainEvent;

/// <summary>Raised when a new feedback is acknowledged and queued for action.</summary>
public sealed record FeedbackTriagedEvent(FeedbackId FeedbackId) : DomainEvent;

/// <summary>Raised when a feedback is closed as resolved.</summary>
public sealed record FeedbackResolvedEvent(FeedbackId FeedbackId) : DomainEvent;

/// <summary>Raised when a feedback is closed without action.</summary>
public sealed record FeedbackDismissedEvent(FeedbackId FeedbackId) : DomainEvent;
