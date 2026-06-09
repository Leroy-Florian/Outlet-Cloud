namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>
/// The commercial plan a subscription runs on. Trial and paid customers share the
/// SAME entitlement-decision path (see <see cref="Entitlements"/>); only the values
/// attached to a tier differ. Adding a tier here never changes the state machine —
/// it just adds a row to the entitlement catalogue.
/// </summary>
public enum PlanTier
{
    /// <summary>The single plan offered today: full hosted private-registry features.</summary>
    Pro = 0,
}
