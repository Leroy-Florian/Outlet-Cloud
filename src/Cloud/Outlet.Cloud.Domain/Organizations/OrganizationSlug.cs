using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// The organization's URL-safe handle, e.g. "acme" in a registry source like
/// <c>https://registry.outlet.dev/acme/</c>. Lowercase kebab-case, unique.
/// </summary>
public sealed class OrganizationSlug : ValueObject
{
    public string Value { get; }

    private OrganizationSlug(string value)
    {
        Value = value;
    }

    public static OrganizationSlug From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("OrganizationSlug cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!normalized.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            throw new ArgumentException(
                $"OrganizationSlug '{value}' must be lowercase kebab-case (a-z, 0-9, '-').", nameof(value));

        if (normalized.StartsWith('-') || normalized.EndsWith('-'))
            throw new ArgumentException(
                $"OrganizationSlug '{value}' must not start or end with '-'.", nameof(value));

        return new OrganizationSlug(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
