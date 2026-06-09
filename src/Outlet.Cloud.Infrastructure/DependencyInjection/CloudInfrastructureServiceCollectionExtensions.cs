using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Infrastructure.Persistence;

namespace Outlet.Cloud.Infrastructure.DependencyInjection;

/// <summary>Composition entry point for the Cloud context's persistence adapters.</summary>
public static class CloudInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Cloud <see cref="CloudDbContext"/> and its repositories. The database
    /// provider is supplied by <paramref name="configureDatabase"/> (PostgreSQL in the host,
    /// SQLite in tests).
    /// </summary>
    public static IServiceCollection AddOutletCloudInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<CloudDbContext>(configureDatabase);
        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
        services.AddScoped<IPublishedItemRepository, EfPublishedItemRepository>();

        return services;
    }
}
