using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>The organization's human-friendly display name.</summary>
public sealed class OrganizationName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private OrganizationName(string value)
    {
        Value = value;
    }

    public static OrganizationName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("OrganizationName cannot be empty.", nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new ArgumentException(
                $"OrganizationName must be at most {MaxLength} characters.", nameof(value));

        return new OrganizationName(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
