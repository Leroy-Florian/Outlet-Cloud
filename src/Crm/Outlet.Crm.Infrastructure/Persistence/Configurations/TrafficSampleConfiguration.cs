using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class TrafficSampleConfiguration : IEntityTypeConfiguration<TrafficSample>
{
    public void Configure(EntityTypeBuilder<TrafficSample> builder)
    {
        builder.ToTable("traffic_samples");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProductId).HasConversion(id => id.Value, value => new Domain.Products.ProductId(value));
        builder.Property(s => s.Path).HasMaxLength(500);
        builder.Property(s => s.ReferrerSource).HasMaxLength(200);
        builder.Property(s => s.UserAgentCategory).HasMaxLength(20);
        builder.Property(s => s.OccurredAt);
        builder.HasIndex(s => s.OccurredAt);
        builder.Ignore(s => s.DomainEvents);
    }
}
