using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class DownloadSnapshotConfiguration : IEntityTypeConfiguration<DownloadSnapshot>
{
    public void Configure(EntityTypeBuilder<DownloadSnapshot> builder)
    {
        builder.ToTable("download_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(s => s.Registry).HasConversion<string>().HasMaxLength(20);
        // Value conversion (not ComplexProperty): EF cannot bind complex types to
        // constructor parameters when materializing immutable aggregates.
        builder.Property(s => s.PackageId)
            .HasConversion(p => p.Value, value => PackageId.Create(value).Value!)
            .HasColumnName("package_id")
            .HasMaxLength(100);
        builder.Property(s => s.TotalDownloads);
        builder.Property(s => s.LatestVersion).HasMaxLength(64);
        builder.Property(s => s.CapturedAt);
        builder.Ignore(s => s.DomainEvents);
    }
}
