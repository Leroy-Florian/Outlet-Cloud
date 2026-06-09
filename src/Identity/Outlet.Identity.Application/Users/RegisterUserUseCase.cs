using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.Users;
using Outlet.Kernel.Shared;

namespace Outlet.Identity.Application.Users;

/// <summary>Command: register a new user with a caller-supplied id.</summary>
public sealed record RegisterUserCommand(Guid UserId, string Email, string DisplayName);

/// <summary>
/// Registers a <see cref="User"/>: validates the value objects, enforces email
/// uniqueness, and persists. Credentials (password, etc.) are set separately
/// through the membership store in Infrastructure.
/// </summary>
public sealed class RegisterUserUseCase(IUserRepository users)
    : IUseCase<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var idResult = Guard.TryBuild(() => UserId.From(command.UserId), "User id is invalid.");
        if (idResult.IsFailure)
            return Result<Guid>.Failure(idResult.Error!);

        var emailResult = Guard.TryBuild(() => EmailAddress.From(command.Email), $"Email '{command.Email}' is invalid.");
        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Error!);

        if (await users.ExistsWithEmailAsync(emailResult.Value!, cancellationToken))
            return Result<Guid>.Failure($"A user with email '{emailResult.Value}' already exists.");

        var userResult = User.Create(idResult.Value!, emailResult.Value!, command.DisplayName);
        if (userResult.IsFailure)
            return Result<Guid>.Failure(userResult.Error!);

        await users.AddAsync(userResult.Value!, cancellationToken);

        return Result<Guid>.Success(idResult.Value!.Value);
    }
}
