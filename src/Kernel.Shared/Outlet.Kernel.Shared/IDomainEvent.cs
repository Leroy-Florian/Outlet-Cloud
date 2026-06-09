namespace Outlet.Kernel.Shared;

/// <summary>
/// INTERFACE FOR DOMAIN EVENTS (DDD Building Block)
///
/// A DOMAIN EVENT represents something significant that HAPPENED in the domain.
/// It captures a fact about the past that domain experts care about.
///
/// KEY CHARACTERISTICS:
/// 1. Named in PAST TENSE: RegistryItemInstalled, ManifestPublished
/// 2. IMMUTABLE: Events are facts; you cannot change the past
/// 3. Contains all information needed to understand what happened
/// 4. Has a unique EventId for idempotency and tracking
/// 5. Has OccurredOn timestamp for ordering and auditing
///
/// WHY DOMAIN EVENTS?
/// 1. DECOUPLING: Bounded contexts communicate through events, not direct calls
/// 2. AUDIT TRAIL: Events can be stored for compliance and debugging
/// 3. EVENTUAL CONSISTENCY: Enable async processing between aggregates
/// 4. CQRS/EVENT SOURCING: Foundation for advanced patterns
///
/// EVENT FLOW:
/// 1. Aggregate root raises event via RaiseDomainEvent()
/// 2. Event is stored in aggregate's pending events list
/// 3. After persistence, Unit of Work dispatches events
/// 4. Event handlers in other bounded contexts react
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this specific event occurrence.
    /// Used for:
    /// - Idempotency: Prevent processing the same event twice
    /// - Correlation: Link related events and logs
    /// - Debugging: Trace event flow across systems
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred (UTC).
    /// Used for:
    /// - Ordering: Process events in correct sequence
    /// - Auditing: Compliance and debugging
    /// - Replay: Event sourcing scenarios
    /// </summary>
    DateTime OccurredOn { get; }
}
