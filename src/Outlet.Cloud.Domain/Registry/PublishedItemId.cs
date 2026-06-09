using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Registry;

/// <summary>Strongly-typed identity of a <see cref="PublishedItem"/>.</summary>
public sealed class PublishedItemId : ValueObject
{
    public Guid Value { get; }

    private PublishedItemId(Guid value)
    {
        Value = value;
    }

    public static PublishedItemId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PublishedItemId cannot be empty.", nameof(value));

        return new PublishedItemId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
