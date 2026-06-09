using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.AccessTokens;

/// <summary>
/// A single permission a token grants, as an OPAQUE lowercase string
/// (e.g. "registry:read", "org:acme:registry:write"). Keeping scopes as plain
/// strings is what lets the Identity context stay free of any Cloud dependency:
/// the Cloud context composes and interprets these strings, Identity only carries
/// and validates their shape.
/// </summary>
public sealed class TokenScope : ValueObject
{
    public string Value { get; }

    private TokenScope(string value)
    {
        Value = value;
    }

    public static TokenScope From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TokenScope cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!normalized.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is ':' or '-' or '_' or '*'))
            throw new ArgumentException(
                $"TokenScope '{value}' may only contain a-z, 0-9 and ':', '-', '_', '*'.", nameof(value));

        return new TokenScope(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
