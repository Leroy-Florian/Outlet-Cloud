using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build <see cref="CloudDbContext"/>
/// without spinning the web host. The connection string is only needed for
/// <c>database update</c>; creating a migration uses the model alone.
/// </summary>
public sealed class CloudDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("OUTLET_CLOUD_CONNECTION")
            ?? "Host=localhost;Database=outlet_cloud;Username=outlet;Password=outlet";

        var options = new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new CloudDbContext(options);
    }
}
