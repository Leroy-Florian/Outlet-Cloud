using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasConversion(id => id.Value, value => new AlertId(value));
        builder.Property(a => a.ProductId).HasConversion(id => id.Value, value => new ProductId(value));
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Message).HasMaxLength(1000);
        builder.Property(a => a.TriggeredAt);
        builder.Property(a => a.Acknowledged);
        builder.Ignore(a => a.DomainEvents);
    }
}
