using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _byId = [];

    public void Seed(User user) => _byId[user.Id.Value] = user;

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(_byId.GetValueOrDefault(id.Value));

    public Task<bool> ExistsWithEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.Any(u => u.Email == email));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        return Task.CompletedTask;
    }
}
