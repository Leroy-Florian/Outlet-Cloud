using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.AccessTokens;

/// <summary>
/// Strongly-typed identity of a <see cref="PersonalAccessToken"/>. This is the public
/// handle of the token (safe to log/list); the secret itself is never modelled here —
/// only its <see cref="TokenHash"/> is stored.
/// </summary>
public sealed class PersonalAccessTokenId : ValueObject
{
    public Guid Value { get; }

    private PersonalAccessTokenId(Guid value)
    {
        Value = value;
    }

    public static PersonalAccessTokenId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PersonalAccessTokenId cannot be empty.", nameof(value));

        return new PersonalAccessTokenId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
