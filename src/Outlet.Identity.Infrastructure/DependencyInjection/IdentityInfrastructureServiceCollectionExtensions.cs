using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
// Npgsql/Sqlite providers are configured by the caller via the options action.
using Outlet.Identity.Application.Ports;
using Outlet.Identity.Infrastructure.Persistence;
using Outlet.Identity.Infrastructure.Security;

namespace Outlet.Identity.Infrastructure.DependencyInjection;

/// <summary>Composition entry point for the Identity context's membership + persistence adapters.</summary>
public static class IdentityInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Identity <see cref="IdentityDataContext"/>, ASP.NET Core Identity
    /// membership over it, the repositories and the token secret factory. The database
    /// provider is supplied by <paramref name="configureDatabase"/> (PostgreSQL in the
    /// host, SQLite in tests) so this layer stays provider-agnostic.
    /// </summary>
    public static IServiceCollection AddOutletIdentityInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<IdentityDataContext>(configureDatabase);

        // AddIdentityCore + EF stores live here (class library). SignInManager and the
        // auth cookie are ASP.NET Core concerns and are added by the web host.
        services.AddIdentityCore<OutletIdentityUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDataContext>();

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IPersonalAccessTokenRepository, EfPersonalAccessTokenRepository>();
        services.AddSingleton<ITokenSecretFactory, Sha256TokenSecretFactory>();

        return services;
    }
}
