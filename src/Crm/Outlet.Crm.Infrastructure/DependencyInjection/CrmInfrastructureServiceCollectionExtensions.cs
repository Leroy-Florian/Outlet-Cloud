using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Infrastructure.PackageStats;
using Outlet.Crm.Infrastructure.Persistence;
using Outlet.Crm.Infrastructure.Persistence.Repositories;

namespace Outlet.Crm.Infrastructure.DependencyInjection;

/// <summary>External stats endpoints consumed by the Crm analytics adapters.</summary>
public sealed record CrmStatsOptions
{
    public string NuGetSearchBaseUrl { get; init; } = "https://azuresearch-usnc.nuget.org/";
    public string NpmApiBaseUrl { get; init; } = "https://api.npmjs.org/";
    public string GitHubApiBaseUrl { get; init; } = "https://api.github.com/";
    public string? GitHubToken { get; init; }
}

/// <summary>Composition entry point for the Crm context's persistence and HTTP adapters.</summary>
public static class CrmInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Crm <see cref="CrmDbContext"/> and its repositories. The database
    /// provider is supplied by <paramref name="configureDatabase"/> (PostgreSQL in the host,
    /// SQLite in tests).
    /// </summary>
    public static IServiceCollection AddOutletCrmInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<CrmDbContext>(configureDatabase);
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
        services.AddScoped<IProspectRepository, EfProspectRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IDownloadSnapshotRepository, EfDownloadSnapshotRepository>();
        services.AddScoped<IRepositorySnapshotRepository, EfRepositorySnapshotRepository>();
        services.AddScoped<IApiMetricRepository, EfApiMetricRepository>();
        services.AddScoped<ITrafficSampleRepository, EfTrafficSampleRepository>();
        services.AddScoped<IFeedbackRepository, EfFeedbackRepository>();

        return services;
    }

    /// <summary>
    /// Registers the typed HTTP clients behind <see cref="IPackageStatsClient"/> and
    /// <see cref="IRepoStatsClient"/> (NuGet azuresearch, npm downloads, GitHub repos).
    /// </summary>
    public static IServiceCollection AddOutletCrmStatsClients(
        this IServiceCollection services,
        CrmStatsOptions options)
    {
        services.AddHttpClient<NuGetStatsHttpClient>(client =>
            client.BaseAddress = new Uri(options.NuGetSearchBaseUrl));
        services.AddHttpClient<NpmStatsHttpClient>(client =>
            client.BaseAddress = new Uri(options.NpmApiBaseUrl));
        services.AddHttpClient<GitHubStatsHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.GitHubApiBaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Outlet-CRM");
            if (!string.IsNullOrWhiteSpace(options.GitHubToken))
            {
                client.DefaultRequestHeaders.Authorization = new("Bearer", options.GitHubToken);
            }
        });

        services.AddScoped<IPackageStatsClient, PackageStatsClient>();
        services.AddScoped<IRepoStatsClient>(sp => sp.GetRequiredService<GitHubStatsHttpClient>());

        return services;
    }
}
