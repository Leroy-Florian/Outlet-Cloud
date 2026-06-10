using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class ProspectConfiguration : IEntityTypeConfiguration<Prospect>
{
    public void Configure(EntityTypeBuilder<Prospect> builder)
    {
        builder.ToTable("prospects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasConversion(id => id.Value, value => new ProspectId(value));
        builder.Property(p => p.ProductId).HasConversion(id => id.Value, value => new Domain.Products.ProductId(value));
        builder.Property(p => p.OrganizationId).HasConversion<Guid?>(
            id => id == null ? null : id.Value.Value,
            value => value == null ? null : new Domain.Organizations.OrganizationId(value.Value));
        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.Company).HasMaxLength(200);
        builder.Property(p => p.Stage).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAt);
        // Value conversion (not ComplexProperty): EF cannot bind complex types to
        // constructor parameters when materializing immutable aggregates.
        builder.Property(p => p.Email)
            .HasConversion(e => e.Value, value => Email.Create(value).Value!)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.OwnsMany(p => p.Interactions, interactions =>
        {
            interactions.ToTable("prospect_interactions");
            interactions.WithOwner().HasForeignKey("prospect_id");
            interactions.HasKey(i => i.Id);
            interactions.Property(i => i.Id).ValueGeneratedNever();
            interactions.Property(i => i.Channel).HasMaxLength(50);
            interactions.Property(i => i.Notes);
            interactions.Property(i => i.OccurredAt);
        });

        builder.Ignore(p => p.DomainEvents);
    }
}
