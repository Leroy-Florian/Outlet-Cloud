using Microsoft.EntityFrameworkCore;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>EF Core context for the Cloud bounded context (organizations, registries, subscriptions).</summary>
public sealed class CloudDbContext(DbContextOptions<CloudDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationRecord> Organizations => Set<OrganizationRecord>();
    public DbSet<MembershipRecord> OrganizationMembers => Set<MembershipRecord>();
    public DbSet<PublishedItemRecord> PublishedItems => Set<PublishedItemRecord>();
    public DbSet<SubscriptionRecord> Subscriptions => Set<SubscriptionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationRecord>(builder =>
        {
            builder.ToTable("organizations");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Slug).IsRequired();
            builder.HasIndex(o => o.Slug).IsUnique();
            builder.Property(o => o.Name).IsRequired();
            builder.Property(o => o.RegistryVisibility)
                .HasConversion<string>()
                .IsRequired()
                .HasDefaultValue(Domain.Organizations.RegistryVisibility.Private);

            builder.HasMany(o => o.Members)
                .WithOne()
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MembershipRecord>(builder =>
        {
            builder.ToTable("organization_members");
            builder.HasKey(m => new { m.OrganizationId, m.UserId });
            builder.Property(m => m.Role).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<PublishedItemRecord>(builder =>
        {
            builder.ToTable("published_items");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Name).IsRequired();
            builder.Property(i => i.ManifestJson).IsRequired();
            builder.HasIndex(i => new { i.OrganizationId, i.Name }).IsUnique();

            builder.HasMany(i => i.Files)
                .WithOne()
                .HasForeignKey(f => f.PublishedItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PublishedFileRecord>(builder =>
        {
            builder.ToTable("published_item_files");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Path).IsRequired();
        });

        modelBuilder.Entity<SubscriptionRecord>(builder =>
        {
            builder.ToTable("subscriptions");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Plan).IsRequired();
            builder.Property(s => s.Status).IsRequired();
            builder.HasIndex(s => s.AccountId).IsUnique();
        });
    }
}
