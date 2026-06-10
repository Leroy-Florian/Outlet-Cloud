using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class RepositorySnapshotConfiguration : IEntityTypeConfiguration<RepositorySnapshot>
{
    public void Configure(EntityTypeBuilder<RepositorySnapshot> builder)
    {
        builder.ToTable("repository_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        // Value conversion (not ComplexProperty): EF cannot bind complex types to
        // constructor parameters when materializing immutable aggregates.
        builder.Property(s => s.Repository)
            .HasConversion(r => r.FullName, value => RepositoryName.Create(value).Value!)
            .HasColumnName("repository")
            .HasMaxLength(200);
        builder.Property(s => s.OpenIssues);
        builder.Property(s => s.Stars);
        builder.Property(s => s.Forks);
        builder.Property(s => s.CapturedAt);
        builder.Ignore(s => s.DomainEvents);
    }
}
