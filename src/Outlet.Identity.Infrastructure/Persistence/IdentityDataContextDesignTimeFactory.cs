using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build <see cref="IdentityDataContext"/>
/// without spinning the web host. The connection string is only needed for
/// <c>database update</c>; creating a migration uses the model alone.
/// </summary>
public sealed class IdentityDataContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDataContext>
{
    public IdentityDataContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("OUTLET_IDENTITY_CONNECTION")
            ?? "Host=localhost;Database=outlet_identity;Username=outlet;Password=outlet";

        var options = new DbContextOptionsBuilder<IdentityDataContext>()
            .UseNpgsql(connection)
            .Options;

        return new IdentityDataContext(options);
    }
}
