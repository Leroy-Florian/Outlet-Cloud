using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Outlet.Cloud.Infrastructure.DependencyInjection;
using Outlet.Cloud.Infrastructure.Persistence;
using Outlet.Cloud.Web.DependencyInjection;
using Outlet.Cloud.Web.Endpoints;
using Outlet.Identity.Infrastructure.DependencyInjection;
using Outlet.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Persistence: PostgreSQL by default. The "Testing" environment lets the test
// factory register the contexts; `Outlet:UseSqlite=true` runs the host standalone
// on local SQLite files (handy for demos without a PostgreSQL server).
var useSqlite = builder.Configuration["Outlet:UseSqlite"] == "true";

if (builder.Environment.IsEnvironment("Testing"))
{
    // The WebApplicationFactory registers both contexts on in-memory SQLite.
}
else if (useSqlite)
{
    builder.Services.AddOutletIdentityInfrastructure(options => options.UseSqlite("Data Source=outlet_identity.db"));
    builder.Services.AddOutletCloudInfrastructure(options => options.UseSqlite("Data Source=outlet_cloud.db"));
}
else
{
    var identityConnection = builder.Configuration.GetConnectionString("Identity")
        ?? "Host=localhost;Database=outlet_identity;Username=outlet;Password=outlet";
    var cloudConnection = builder.Configuration.GetConnectionString("Cloud")
        ?? "Host=localhost;Database=outlet_cloud;Username=outlet;Password=outlet";

    builder.Services.AddOutletIdentityInfrastructure(options => options.UseNpgsql(identityConnection));
    builder.Services.AddOutletCloudInfrastructure(options => options.UseNpgsql(cloudConnection));
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.AddAuthorization();

// SignInManager / auth cookie are ASP.NET Core concerns, added on top of the
// membership core registered by AddOutletIdentityInfrastructure. Default token
// providers back the password-reset flow.
new IdentityBuilder(typeof(OutletIdentityUser), typeof(IdentityRole<Guid>), builder.Services)
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddOutletCloudWeb();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (useSqlite)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<IdentityDataContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<CloudDbContext>().Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapOutletCloud();
app.MapOutletAuth();
app.MapOrganizationManagement();

app.Run();

/// <summary>Exposed so integration tests can spin the host via WebApplicationFactory.</summary>
public partial class Program;
