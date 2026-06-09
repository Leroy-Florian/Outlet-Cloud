using Outlet.Identity.Domain.Users;
using Outlet.Kernel.Shared;

namespace Outlet.Identity.Domain.AccessTokens;

/// <summary>
/// AGGREGATE ROOT — a machine credential a user issues for the CLI / CI to call
/// Outlet with, à la GitHub PAT. The secret is shown once and only its
/// <see cref="TokenHash"/> is kept here. A token carries opaque <see cref="TokenScope"/>s
/// (what it may do) and is the bridge between the web UI (which mints it via SSO)
/// and the CLI (which presents it as a bearer credential).
///
/// Invariants enforced here:
/// - A token always has a non-empty label and at least one scope.
/// - An expiry, when set, is strictly after issuance.
/// - A revoked token can never be revoked again, and never authenticates.
///
/// Time is never read ambiently: issuance/expiry/revocation timestamps are passed
/// in by the Application layer (via ICurrentDateTimeProvider).
/// </summary>
public sealed class PersonalAccessToken : AggregateRoot<PersonalAccessTokenId>
{
    public UserId OwnerId { get; }
    public string Name { get; }
    public TokenHash Hash { get; }

    private readonly List<TokenScope> _scopes;
    public IReadOnlyList<TokenScope> Scopes => _scopes;

    public DateTime CreatedAtUtc { get; }
    public DateTime? ExpiresAtUtc { get; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;

    private PersonalAccessToken(
        PersonalAccessTokenId id,
        UserId ownerId,
        string name,
        TokenHash hash,
        IEnumerable<TokenScope> scopes,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc)
        : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        Hash = hash;
        _scopes = [.. scopes];
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static Result<PersonalAccessToken> Create(
        PersonalAccessTokenId id,
        UserId ownerId,
        string name,
        TokenHash hash,
        IReadOnlyCollection<TokenScope> scopes,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<PersonalAccessToken>.Failure("Personal access token must have a name.");

        if (scopes.Count == 0)
            return Result<PersonalAccessToken>.Failure("Personal access token must grant at least one scope.");

        if (expiresAtUtc is { } expiry && expiry <= createdAtUtc)
            return Result<PersonalAccessToken>.Failure("Personal access token expiry must be after its creation.");

        var token = new PersonalAccessToken(id, ownerId, name.Trim(), hash, scopes.Distinct(), createdAtUtc, expiresAtUtc);
        token.RaiseDomainEvent(new PersonalAccessTokenIssuedEvent(id, ownerId));

        return Result<PersonalAccessToken>.Success(token);
    }

    /// <summary>
    /// Rehydrates a token from TRUSTED persistence without raising events or
    /// re-running issuance guards. Infrastructure-only entry point.
    /// </summary>
    public static PersonalAccessToken Restore(
        PersonalAccessTokenId id,
        UserId ownerId,
        string name,
        TokenHash hash,
        IEnumerable<TokenScope> scopes,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc,
        DateTime? revokedAtUtc)
    {
        var token = new PersonalAccessToken(id, ownerId, name, hash, scopes, createdAtUtc, expiresAtUtc)
        {
            RevokedAtUtc = revokedAtUtc,
        };

        return token;
    }

    /// <summary>True when the token is neither revoked nor expired at <paramref name="nowUtc"/>.</summary>
    public bool IsValidAt(DateTime nowUtc) =>
        !IsRevoked && (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc);

    /// <summary>Whether this token grants <paramref name="scope"/>.</summary>
    public bool HasScope(TokenScope scope) => _scopes.Contains(scope);

    public Result Revoke(DateTime revokedAtUtc)
    {
        if (IsRevoked)
            return Result.Failure("Personal access token is already revoked.");

        RevokedAtUtc = revokedAtUtc;
        RaiseDomainEvent(new PersonalAccessTokenRevokedEvent(Id));

        return Result.Success();
    }
}
