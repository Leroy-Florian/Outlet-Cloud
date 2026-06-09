using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Subscriptions;

/// <summary>Strongly-typed identity of a <see cref="Subscription"/>.</summary>
public sealed class SubscriptionId : ValueObject
{
    public Guid Value { get; }

    private SubscriptionId(Guid value)
    {
        Value = value;
    }

    public static SubscriptionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SubscriptionId cannot be empty.", nameof(value));

        return new SubscriptionId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
