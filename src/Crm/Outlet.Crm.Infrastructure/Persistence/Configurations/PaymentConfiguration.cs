using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Payments;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ProductId).HasConversion(id => id.Value, value => new Domain.Products.ProductId(value));
        builder.Property(p => p.OrganizationId).HasConversion<Guid?>(
            id => id == null ? null : id.Value.Value,
            value => value == null ? null : new Domain.Organizations.OrganizationId(value.Value));
        builder.Property(p => p.Source).HasMaxLength(50);
        builder.Property(p => p.ExternalReference).HasMaxLength(200);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        // Value conversion (not ComplexProperty): EF cannot bind complex types to
        // constructor parameters when materializing immutable aggregates. Money is
        // stored as "<invariant amount> <ISO currency>" in a single column; the API
        // never queries on the amount, it only materializes it.
        builder.Property(p => p.Amount)
            .HasConversion(
                m => m.Amount.ToString(CultureInfo.InvariantCulture) + " " + m.Currency,
                value => ParseMoney(value))
            .HasColumnName("amount")
            .HasMaxLength(32);

        builder.Property(p => p.CreatedAt);
        builder.Ignore(p => p.DomainEvents);
    }

    private static Money ParseMoney(string value)
    {
        var separator = value.LastIndexOf(' ');
        return Money.Create(
            decimal.Parse(value[..separator], CultureInfo.InvariantCulture),
            value[(separator + 1)..]).Value!;
    }
}
