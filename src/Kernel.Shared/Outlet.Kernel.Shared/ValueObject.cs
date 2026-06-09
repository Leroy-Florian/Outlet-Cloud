using System.Diagnostics.CodeAnalysis;

namespace Outlet.Kernel.Shared;

/// <summary>
/// BASE CLASS FOR VALUE OBJECTS (DDD Building Block)
///
/// In Domain-Driven Design, a VALUE OBJECT is an object that:
/// 1. Has NO unique identity - it IS its attributes
/// 2. Is IMMUTABLE - once created, it cannot change
/// 3. Is compared by its ATTRIBUTES, not by identity
/// 4. Can be freely replaced by another instance with the same values
///
/// VALUE OBJECTS vs ENTITIES:
/// - Entity: "I am RegistryItem #email-smtp" (identity matters)
/// - Value Object: "I am the concern 'email'" (attributes matter)
///
/// WHY USE VALUE OBJECTS?
/// 1. Type safety: ConcernName.Create("email") vs raw string
/// 2. Validation: Invalid values are rejected at construction
/// 3. Encapsulation: Business rules live inside the value object
/// 4. Immutability: No accidental state changes
/// 5. Self-documenting: TargetNamespace is clearer than a string
///
/// IMPLEMENTATION PATTERN:
/// - Private constructor prevents invalid instances
/// - Static factory methods validate and create instances
/// - GetEqualityComponents() defines which properties determine equality
/// </summary>
[SuppressMessage("Major Code Smell", "S4035:Classes implementing \"IEquatable<T>\" should be sealed",
    Justification = "Abstract DDD base; subclasses inherit structural equality by design.")]
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the components used for equality comparison.
    ///
    /// Override this in derived classes to specify which properties
    /// should be compared when checking equality.
    ///
    /// Example for a PackageDependency value object:
    ///   yield return PackageId;
    ///   yield return MinimumVersion;
    ///
    /// Two PackageDependency objects are equal if both Id AND version match.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// ATTRIBUTE-BASED EQUALITY:
    /// Two value objects are equal if ALL their equality components match.
    /// This is the opposite of entities, which compare by identity.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Hash code computed from all equality components.
    /// Ensures consistent behavior in hash-based collections.
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
