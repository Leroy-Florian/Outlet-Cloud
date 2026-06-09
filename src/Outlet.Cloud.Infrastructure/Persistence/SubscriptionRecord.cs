namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>EF persistence model for an organization's subscription.</summary>
public sealed class SubscriptionRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Plan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>Trial window; null once the subscription was never on a trial (not used today).</summary>
    public DateOnly? TrialStartedOn { get; set; }
    public DateOnly? TrialEndsOn { get; set; }
}
