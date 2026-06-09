using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Kernel.Shared;

namespace Outlet.Identity.Application.AccessTokens;

/// <summary>Command: revoke a personal access token so it can no longer authenticate.</summary>
public sealed record RevokePersonalAccessTokenCommand(Guid TokenId);

/// <summary>Revokes a <see cref="PersonalAccessToken"/>. Idempotency is the aggregate's call: re-revoking fails.</summary>
public sealed class RevokePersonalAccessTokenUseCase(
    IPersonalAccessTokenRepository tokens,
    ICurrentDateTimeProvider clock)
    : IUseCase<RevokePersonalAccessTokenCommand>
{
    public async Task<Result> HandleAsync(RevokePersonalAccessTokenCommand command, CancellationToken cancellationToken = default)
    {
        var idResult = Guard.TryBuild(() => PersonalAccessTokenId.From(command.TokenId), "Token id is invalid.");
        if (idResult.IsFailure)
            return Result.Failure(idResult.Error!);

        var token = await tokens.GetByIdAsync(idResult.Value!, cancellationToken);
        if (token is null)
            return Result.Failure($"Personal access token '{idResult.Value}' was not found.");

        var revoke = token.Revoke(clock.UtcNow);
        if (revoke.IsFailure)
            return revoke;

        await tokens.UpdateAsync(token, cancellationToken);

        return Result.Success();
    }
}
