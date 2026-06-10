using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.ApiMetrics;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class ApiMetricSampleConfiguration : IEntityTypeConfiguration<ApiMetricSample>
{
    public void Configure(EntityTypeBuilder<ApiMetricSample> builder)
    {
        builder.ToTable("api_metric_samples");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProductId).HasConversion(id => id.Value, value => new Domain.Products.ProductId(value));
        builder.Property(s => s.Endpoint).HasMaxLength(300);
        builder.Property(s => s.StatusCode);
        builder.Property(s => s.DurationMs);
        builder.Property(s => s.OccurredAt);
        builder.HasIndex(s => s.OccurredAt);
        builder.Ignore(s => s.DomainEvents);
    }
}
