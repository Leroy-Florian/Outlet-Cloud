namespace Outlet.Kernel.Shared;

/// <summary>
/// BASE RECORD FOR DOMAIN EVENTS
///
/// Provides common implementation for all domain events:
/// - Automatic EventId generation (Guid.NewGuid)
/// - Automatic OccurredOn timestamp (DateTime.UtcNow)
///
/// WHY A RECORD?
/// Records in C# are ideal for domain events because:
/// 1. IMMUTABLE by default - events represent facts that cannot change
/// 2. Value-based equality - two events with same data are equal
/// 3. Built-in ToString() - useful for logging and debugging
/// 4. Concise syntax with positional parameters
///
/// INHERITANCE:
/// Concrete events inherit from this base and add their specific data:
///
///   public sealed record RegistryItemInstalled(
///       string ItemName,
///       string TargetProject) : DomainEvent;
///
/// The base class provides EventId and OccurredOn automatically.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Auto-generated unique identifier for this event instance.
    /// Each event occurrence gets its own EventId.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Auto-captured timestamp when the event was created.
    /// Always in UTC to avoid timezone issues in distributed systems.
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
