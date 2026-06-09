using System.Diagnostics.CodeAnalysis;

namespace Outlet.Kernel.Shared;

/// <summary>
/// BASE CLASS FOR DOMAIN ENTITIES (DDD Building Block)
///
/// In Domain-Driven Design, an Entity is an object that:
/// 1. Has a unique IDENTITY that persists through time and state changes
/// 2. Is distinguished by its identity, NOT by its attributes
/// 3. Can change its attributes while remaining the same entity
///
/// IDENTITY vs EQUALITY:
/// - Two entities with the same Id are considered EQUAL (same entity)
/// - Two entities with different Ids are DIFFERENT, even if all other attributes match
///
/// This contrasts with VALUE OBJECTS, which are compared by their attributes.
///
/// GENERIC TYPE PARAMETER:
/// - TId allows flexibility in identity type (Guid, int, string, strongly-typed Id)
/// - The 'notnull' constraint ensures the identity cannot be null
/// </summary>
/// <typeparam name="TId">The type of the entity's unique identifier</typeparam>
[SuppressMessage("Major Code Smell", "S4035:Classes implementing \"IEquatable<T>\" should be sealed",
    Justification = "Abstract DDD base; subclasses inherit identity-based equality by design.")]
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// The unique identifier of this entity.
    /// This is the ONLY property used for equality comparison.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Protected constructor ensures entities are created through
    /// factory methods or aggregate roots, enforcing proper instantiation.
    /// </summary>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// IDENTITY-BASED EQUALITY:
    /// Two entities are equal if and only if they have the same Id.
    /// This is fundamental to DDD - entities are defined by their identity.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Hash code based on identity ensures consistent behavior
    /// when entities are used in hash-based collections (Dictionary, HashSet).
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Operator overloads provide natural equality syntax:
    /// if (entity1 == entity2) { ... }
    /// </summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
