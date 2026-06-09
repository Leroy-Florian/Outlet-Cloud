using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>
/// Strongly-typed identity of the ACCOUNT a subscription belongs to. An account maps 1:1 to
/// an Identity user; the link crosses the bounded-context boundary as a plain GUID (never the
/// Identity type), exactly like <c>MemberUserId</c>. The trial/plan is an account concern —
/// an account's organizations host their private registries under the account's subscription.
/// </summary>
public sealed class AccountId : ValueObject
{
    public Guid Value { get; }

    private AccountId(Guid value)
    {
        Value = value;
    }

    public static AccountId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AccountId cannot be empty.", nameof(value));

        return new AccountId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
