using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Releases;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class ReleaseRecordConfiguration : IEntityTypeConfiguration<ReleaseRecord>
{
    public void Configure(EntityTypeBuilder<ReleaseRecord> builder)
    {
        builder.ToTable("releases");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion(id => id.Value, value => new ReleaseId(value));
        builder.Property(r => r.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        // Value conversion (not ComplexProperty): EF cannot bind complex types to
        // constructor parameters when materializing immutable aggregates.
        builder.Property(r => r.Repository)
            .HasConversion(r => r.FullName, value => RepositoryName.Create(value).Value!)
            .HasColumnName("repository")
            .HasMaxLength(200);
        builder.Property(r => r.TagName).HasMaxLength(200);
        builder.Property(r => r.Name).HasMaxLength(500);
        builder.Property(r => r.PublishedAt);
        builder.HasIndex(r => new { r.ProductId, r.Repository, r.TagName }).IsUnique();
        builder.Ignore(r => r.DomainEvents);
    }
}
