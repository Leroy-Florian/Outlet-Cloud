using Microsoft.EntityFrameworkCore;
using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>SECONDARY ADAPTER — EF Core / ASP.NET Core Identity implementation of <see cref="IUserRepository"/>.</summary>
public sealed class EfUserRepository(IdentityDataContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var record = await db.Users.FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public Task<bool> ExistsWithEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Value.ToUpperInvariant();
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var email = user.Email.Value;
        var normalized = email.ToUpperInvariant();

        db.Users.Add(new OutletIdentityUser
        {
            Id = user.Id.Value,
            DisplayName = user.DisplayName,
            Email = email,
            NormalizedEmail = normalized,
            UserName = email,
            NormalizedUserName = normalized,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User ToDomain(OutletIdentityUser record) =>
        User.Restore(UserId.From(record.Id), EmailAddress.From(record.Email!), record.DisplayName);
}
