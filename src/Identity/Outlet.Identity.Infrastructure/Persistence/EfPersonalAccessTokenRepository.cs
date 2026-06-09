using Microsoft.EntityFrameworkCore;
using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>SECONDARY ADAPTER — EF Core implementation of <see cref="IPersonalAccessTokenRepository"/>.</summary>
public sealed class EfPersonalAccessTokenRepository(IdentityDataContext db) : IPersonalAccessTokenRepository
{
    public async Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default)
    {
        db.PersonalAccessTokens.Add(ToRecord(token));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PersonalAccessToken?> GetByIdAsync(PersonalAccessTokenId id, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Id == id.Value, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PersonalAccessToken?> FindByHashAsync(TokenHash hash, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Hash == hash.Value, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PersonalAccessToken>> ListForOwnerAsync(UserId ownerId, CancellationToken cancellationToken = default)
    {
        var records = await db.PersonalAccessTokens
            .Where(t => t.OwnerId == ownerId.Value)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task UpdateAsync(PersonalAccessToken token, CancellationToken cancellationToken = default)
    {
        var record = await db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Id == token.Id.Value, cancellationToken);
        if (record is null)
        {
            db.PersonalAccessTokens.Add(ToRecord(token));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        record.RevokedAtUtc = token.RevokedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static PersonalAccessTokenRecord ToRecord(PersonalAccessToken token) =>
        new()
        {
            Id = token.Id.Value,
            OwnerId = token.OwnerId.Value,
            Name = token.Name,
            Hash = token.Hash.Value,
            Scopes = string.Join(' ', token.Scopes.Select(s => s.Value)),
            CreatedAtUtc = token.CreatedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            RevokedAtUtc = token.RevokedAtUtc,
        };

    private static PersonalAccessToken ToDomain(PersonalAccessTokenRecord record) =>
        PersonalAccessToken.Restore(
            PersonalAccessTokenId.From(record.Id),
            UserId.From(record.OwnerId),
            record.Name,
            TokenHash.From(record.Hash),
            [.. record.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(TokenScope.From)],
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.RevokedAtUtc);
}
