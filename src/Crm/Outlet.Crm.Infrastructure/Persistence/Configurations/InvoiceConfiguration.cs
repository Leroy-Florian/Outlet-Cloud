using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outlet.Crm.Domain.Invoices;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasConversion(id => id.Value, value => new InvoiceId(value));
        builder.Property(i => i.InvoiceNumber).HasMaxLength(20);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.Property(i => i.CustomerName).HasMaxLength(200);
        builder.Property(i => i.CustomerEmail).HasConversion(
            email => email == null ? null : email.Value,
            value => value == null ? null : Email.Create(value).Value);
        builder.Property(i => i.CustomerAddress).HasMaxLength(500);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.CreatedAt);
        builder.Property(i => i.IssuedAt);
        builder.Property(i => i.PaidAt);
        builder.Property(i => i.PaymentId);
        builder.Ignore(i => i.Currency);
        builder.Ignore(i => i.Total);

        builder.OwnsMany(i => i.Lines, lines =>
        {
            lines.ToTable("invoice_lines");
            lines.WithOwner().HasForeignKey("invoice_id");
            lines.HasKey(l => l.Id);
            lines.Property(l => l.Id).ValueGeneratedNever();
            lines.Property(l => l.Description).HasMaxLength(500);
            lines.Property(l => l.Quantity);

            // Same single-column Money convention as PaymentConfiguration.
            lines.Property(l => l.UnitPrice)
                .HasConversion(
                    m => m.Amount.ToString(CultureInfo.InvariantCulture) + " " + m.Currency,
                    value => ParseMoney(value))
                .HasColumnName("unit_price")
                .HasMaxLength(32);

            lines.Ignore(l => l.LineTotal);
        });

        builder.Ignore(i => i.DomainEvents);
    }

    private static Money ParseMoney(string value)
    {
        var separator = value.LastIndexOf(' ');
        return Money.Create(
            decimal.Parse(value[..separator], CultureInfo.InvariantCulture),
            value[(separator + 1)..]).Value!;
    }
}
