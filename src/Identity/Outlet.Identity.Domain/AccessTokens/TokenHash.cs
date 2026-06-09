using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.AccessTokens;

/// <summary>
/// The hex digest of a personal access token secret. The domain NEVER sees the
/// plaintext secret: it is generated and hashed in Infrastructure, shown to the
/// user exactly once, and only this digest is persisted. Validating a presented
/// token = hashing it and comparing against this value.
/// </summary>
public sealed class TokenHash : ValueObject
{
    public string Value { get; }

    private TokenHash(string value)
    {
        Value = value;
    }

    public static TokenHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TokenHash cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length % 2 != 0 || !normalized.All(Uri.IsHexDigit))
            throw new ArgumentException($"TokenHash '{value}' must be an even-length hex digest.", nameof(value));

        return new TokenHash(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
