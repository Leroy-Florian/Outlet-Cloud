using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.Users;

/// <summary>
/// Strongly-typed identity of a <see cref="User"/>. Backed by a GUID so it can be
/// minted independently of any store. Other bounded contexts (Cloud) reference a
/// user only through this id, never through the <see cref="User"/> aggregate.
/// </summary>
public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(value));

        return new UserId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
