using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Identity bounded context: the ASP.NET Core Identity
/// schema (users, roles, …) plus the personal access tokens table.
/// </summary>
public sealed class IdentityDataContext(DbContextOptions<IdentityDataContext> options)
    : IdentityDbContext<OutletIdentityUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<PersonalAccessTokenRecord> PersonalAccessTokens => Set<PersonalAccessTokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutletIdentityUser>()
            .Property(u => u.DisplayName)
            .IsRequired();

        modelBuilder.Entity<PersonalAccessTokenRecord>(builder =>
        {
            builder.ToTable("personal_access_tokens");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired();
            builder.Property(t => t.Hash).IsRequired();
            builder.Property(t => t.Scopes).IsRequired();
            builder.HasIndex(t => t.Hash).IsUnique();
            builder.HasIndex(t => t.OwnerId);
        });
    }
}
