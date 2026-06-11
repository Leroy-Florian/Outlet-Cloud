using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedback");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasConversion(id => id.Value, value => new FeedbackId(value));
        builder.Property(f => f.ProductId).HasConversion(id => id.Value, value => new Domain.Products.ProductId(value));
        builder.Property(f => f.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Message).HasMaxLength(4000);
        builder.Property(f => f.SourceApp).HasMaxLength(200);
        // Value conversion (not ComplexProperty): EF cannot bind complex types to
        // constructor parameters when materializing immutable aggregates.
        builder.Property(f => f.ReporterEmail)
            .HasConversion<string?>(
                e => e == null ? null : e.Value,
                value => value == null ? null : Email.Create(value).Value!)
            .HasColumnName("reporter_email")
            .HasMaxLength(320);
        builder.Property(f => f.Score);
        builder.Property(f => f.ReceivedAt);
        builder.HasIndex(f => f.ProductId);
        builder.HasIndex(f => f.Status);
        builder.Ignore(f => f.DomainEvents);
    }
}
