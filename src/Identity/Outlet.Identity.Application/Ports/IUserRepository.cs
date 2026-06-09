using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="User"/> aggregates.</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
