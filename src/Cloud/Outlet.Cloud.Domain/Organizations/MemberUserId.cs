using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// Reference to a user that lives in the Identity bounded context, carried here by
/// id only. Cloud deliberately does NOT import Identity's <c>UserId</c> type: a
/// local id VO is what keeps the two contexts decoupled (and testably isolated).
/// </summary>
public sealed class MemberUserId : ValueObject
{
    public Guid Value { get; }

    private MemberUserId(Guid value)
    {
        Value = value;
    }

    public static MemberUserId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("MemberUserId cannot be empty.", nameof(value));

        return new MemberUserId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
