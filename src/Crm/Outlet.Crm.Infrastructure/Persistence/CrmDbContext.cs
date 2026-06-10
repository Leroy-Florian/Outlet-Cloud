using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Invoices;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Infrastructure.Persistence;

/// <summary>EF Core context for the Crm bounded context (products, prospects, payments, analytics).</summary>
public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Prospect> Prospects => Set<Prospect>();

    public DbSet<RepositorySnapshot> RepositorySnapshots => Set<RepositorySnapshot>();

    public DbSet<DownloadSnapshot> DownloadSnapshots => Set<DownloadSnapshot>();

    public DbSet<ApiMetricSample> ApiMetricSamples => Set<ApiMetricSample>();

    public DbSet<TrafficSample> TrafficSamples => Set<TrafficSample>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Feedback> FeedbackItems => Set<Feedback>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<Objective> Objectives => Set<Objective>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);
}
