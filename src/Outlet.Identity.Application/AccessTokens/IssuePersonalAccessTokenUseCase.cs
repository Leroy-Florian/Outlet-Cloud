using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;
using Outlet.Kernel.Shared;

namespace Outlet.Identity.Application.AccessTokens;

/// <summary>Command: issue a personal access token for a user, scoped by opaque scope strings.</summary>
public sealed record IssuePersonalAccessTokenCommand(
    Guid OwnerUserId,
    string Name,
    IReadOnlyList<string> Scopes,
    DateTime? ExpiresAtUtc = null);

/// <summary>The result of issuing a token. <see cref="Secret"/> is the one-time plaintext — it is never retrievable again.</summary>
public sealed record IssuedPersonalAccessToken(Guid TokenId, string Secret, string Name, DateTime? ExpiresAtUtc);

/// <summary>
/// Issues a <see cref="PersonalAccessToken"/>: verifies the owner exists, validates
/// the scopes, mints a secret via <see cref="ITokenSecretFactory"/>, persists only
/// the hash, and returns the plaintext secret exactly once.
///
/// Scopes arrive already composed (the Cloud context derives them from membership
/// before calling this use case), keeping Identity free of any Cloud dependency.
/// </summary>
public sealed class IssuePersonalAccessTokenUseCase(
    IUserRepository users,
    IPersonalAccessTokenRepository tokens,
    ITokenSecretFactory secretFactory,
    ICurrentDateTimeProvider clock)
    : IUseCase<IssuePersonalAccessTokenCommand, IssuedPersonalAccessToken>
{
    public async Task<Result<IssuedPersonalAccessToken>> HandleAsync(
        IssuePersonalAccessTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownerResult = Guard.TryBuild(() => UserId.From(command.OwnerUserId), "Owner user id is invalid.");
        if (ownerResult.IsFailure)
            return Result<IssuedPersonalAccessToken>.Failure(ownerResult.Error!);

        var owner = await users.GetByIdAsync(ownerResult.Value!, cancellationToken);
        if (owner is null)
            return Result<IssuedPersonalAccessToken>.Failure($"User '{ownerResult.Value}' does not exist.");

        var scopes = new List<TokenScope>();
        foreach (var raw in command.Scopes)
        {
            var scopeResult = Guard.TryBuild(() => TokenScope.From(raw), $"Scope '{raw}' is invalid.");
            if (scopeResult.IsFailure)
                return Result<IssuedPersonalAccessToken>.Failure(scopeResult.Error!);

            scopes.Add(scopeResult.Value!);
        }

        var secret = secretFactory.Create();

        var tokenResult = PersonalAccessToken.Create(
            secret.Id,
            ownerResult.Value!,
            command.Name,
            secret.Hash,
            scopes,
            clock.UtcNow,
            command.ExpiresAtUtc);

        if (tokenResult.IsFailure)
            return Result<IssuedPersonalAccessToken>.Failure(tokenResult.Error!);

        await tokens.AddAsync(tokenResult.Value!, cancellationToken);

        return Result<IssuedPersonalAccessToken>.Success(new IssuedPersonalAccessToken(
            secret.Id.Value,
            secret.Secret,
            tokenResult.Value!.Name,
            tokenResult.Value.ExpiresAtUtc));
    }
}
