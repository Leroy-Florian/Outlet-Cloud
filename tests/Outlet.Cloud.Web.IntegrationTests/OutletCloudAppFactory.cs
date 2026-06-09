using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Cloud.Infrastructure.DependencyInjection;
using Outlet.Cloud.Infrastructure.Persistence;
using Outlet.Identity.Infrastructure.DependencyInjection;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.IntegrationTests;

/// <summary>
/// Spins the real Outlet Cloud host in-process under the "Testing" environment (so the
/// host skips PostgreSQL), registering both contexts on in-memory SQLite instead. The
/// connections are kept open for the factory's lifetime so the schema survives.
/// </summary>
public sealed class OutletCloudAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _identityConnection = new("DataSource=:memory:");
    private readonly SqliteConnection _cloudConnection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _identityConnection.Open();
        _cloudConnection.Open();

        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddOutletIdentityInfrastructure(options => options.UseSqlite(_identityConnection));
            services.AddOutletCloudInfrastructure(options => options.UseSqlite(_cloudConnection));
        });
    }

    /// <summary>Creates both schemas. Call once before issuing requests.</summary>
    public OutletCloudAppFactory Migrated()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentityDataContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<CloudDbContext>().Database.EnsureCreated();
        return this;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _identityConnection.Dispose();
            _cloudConnection.Dispose();
        }
    }
}
