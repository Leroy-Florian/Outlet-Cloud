namespace Outlet.Kernel.Shared;

/// <summary>
/// BASE CLASS FOR AGGREGATE ROOTS (DDD Building Block)
///
/// In Domain-Driven Design, an AGGREGATE is a cluster of domain objects that:
/// 1. Are treated as a single unit for data changes
/// 2. Have a root entity (the Aggregate Root) that controls access
/// 3. Maintain consistency boundaries (invariants)
///
/// The AGGREGATE ROOT is the single entry point to the aggregate:
/// - External objects can only reference the root, never internal entities
/// - All changes to the aggregate go through the root
/// - The root is responsible for enforcing all invariants
///
/// DOMAIN EVENTS:
/// - Aggregate roots can raise domain events to communicate state changes
/// - Events are collected and dispatched AFTER the transaction commits
/// - This enables loose coupling between bounded contexts
///
/// CONSISTENCY BOUNDARY:
/// - Everything inside an aggregate is immediately consistent
/// - Consistency between aggregates is EVENTUALLY consistent (via events)
///
/// Example: RegistryItem aggregate contains the item identity, its files and
/// its NuGet dependencies. You cannot mutate those parts directly; you must
/// go through the RegistryItem root.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's unique identifier</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    /// <summary>
    /// Collection of domain events raised by this aggregate.
    /// Events are stored temporarily until they can be dispatched
    /// after the aggregate is persisted (typically by the Unit of Work).
    ///
    /// Using a List internally but exposing as IReadOnlyCollection
    /// follows the principle of minimizing the public interface.
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Exposes domain events as read-only collection.
    /// External code can read events but cannot modify the list.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event from within the aggregate.
    ///
    /// Domain events represent something that HAPPENED in the domain.
    /// They are named in past tense: RegistryItemInstalled, ManifestPublished, etc.
    ///
    /// Events should be raised when domain rules are satisfied and
    /// the state change is valid - typically at the end of a successful operation.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events.
    ///
    /// Called by infrastructure code (Unit of Work, Event Dispatcher)
    /// AFTER events have been dispatched successfully.
    /// This prevents events from being dispatched multiple times.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
