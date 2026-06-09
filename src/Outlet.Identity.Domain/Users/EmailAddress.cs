using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.Users;

/// <summary>
/// A user's email address, normalised to lowercase. Validation is intentionally
/// minimal (one '@', a dotted domain) — the authoritative check is delivery, and
/// the membership store (ASP.NET Core Identity, in Infrastructure) owns uniqueness.
/// </summary>
public sealed class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("EmailAddress cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        var at = normalized.IndexOf('@');
        if (at <= 0 || at != normalized.LastIndexOf('@') || at == normalized.Length - 1)
            throw new ArgumentException($"EmailAddress '{value}' is not a valid email.", nameof(value));

        var domain = normalized[(at + 1)..];
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
            throw new ArgumentException($"EmailAddress '{value}' has an invalid domain.", nameof(value));

        return new EmailAddress(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
