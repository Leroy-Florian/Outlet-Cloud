using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class ObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> builder)
    {
        builder.ToTable("objectives");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasConversion(id => id.Value, value => new ObjectiveId(value));
        builder.Property(o => o.ProductId).HasConversion<Guid?>(
            id => id == null ? null : id.Value.Value,
            value => value == null ? null : new ProductId(value.Value));
        builder.Property(o => o.Metric).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.TargetValue);

        // Stored as the DateTime of the month's first day: portable across SQLite
        // (tests) and PostgreSQL without provider-specific DateOnly handling.
        builder.Property(o => o.Month)
            .HasConversion(month => month.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value));

        builder.Property(o => o.CreatedAt);
        builder.HasIndex(o => new { o.ProductId, o.Metric, o.Month }).IsUnique();
        builder.Ignore(o => o.DomainEvents);
    }
}
