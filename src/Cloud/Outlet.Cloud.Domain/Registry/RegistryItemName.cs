using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Registry;

/// <summary>
/// The name of a published registry item (e.g. "email-smtp"). Lowercase kebab-case;
/// it is the item's key within an organization's registry and the path segment the
/// CLI fetches files under.
/// </summary>
public sealed class RegistryItemName : ValueObject
{
    public string Value { get; }

    private RegistryItemName(string value)
    {
        Value = value;
    }

    public static RegistryItemName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("RegistryItemName cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!normalized.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            throw new ArgumentException(
                $"RegistryItemName '{value}' must be lowercase kebab-case (a-z, 0-9, '-').", nameof(value));

        if (normalized.StartsWith('-') || normalized.EndsWith('-'))
            throw new ArgumentException($"RegistryItemName '{value}' must not start or end with '-'.", nameof(value));

        return new RegistryItemName(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
