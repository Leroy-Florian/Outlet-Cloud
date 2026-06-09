using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>Strongly-typed identity of an <see cref="Organization"/>.</summary>
public sealed class OrganizationId : ValueObject
{
    public Guid Value { get; }

    private OrganizationId(Guid value)
    {
        Value = value;
    }

    public static OrganizationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty.", nameof(value));

        return new OrganizationId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
