using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Outlet.Crm.Infrastructure.DependencyInjection;
using Outlet.Crm.Infrastructure.Persistence;
using Outlet.Crm.Web.DependencyInjection;
using Outlet.Crm.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Persistence: PostgreSQL by default. The "Testing" environment lets a test factory
// register the context; `Outlet:UseSqlite=true` runs the host standalone on a local
// SQLite file (handy for demos without a PostgreSQL server).
var useSqlite = builder.Configuration["Outlet:UseSqlite"] == "true";

if (builder.Environment.IsEnvironment("Testing"))
{
    // The WebApplicationFactory registers the context on in-memory SQLite.
}
else if (useSqlite)
{
    builder.Services.AddOutletCrmInfrastructure(options => options.UseSqlite("Data Source=outlet_crm.db"));
}
else
{
    var crmConnection = builder.Configuration.GetConnectionString("Crm")
        ?? "Host=localhost;Database=outlet_crm;Username=outlet;Password=outlet";

    builder.Services.AddOutletCrmInfrastructure(options => options.UseNpgsql(crmConnection));
}

builder.Services.AddOutletCrmStatsClients(new CrmStatsOptions
{
    NuGetSearchBaseUrl = builder.Configuration["NuGet:SearchBaseUrl"] ?? "https://azuresearch-usnc.nuget.org/",
    NuGetFlatContainerBaseUrl = builder.Configuration["NuGet:FlatContainerBaseUrl"] ?? "https://api.nuget.org/v3-flatcontainer/",
    NpmRegistryBaseUrl = builder.Configuration["Npm:RegistryBaseUrl"] ?? "https://registry.npmjs.org/",
    NpmApiBaseUrl = builder.Configuration["Npm:ApiBaseUrl"] ?? "https://api.npmjs.org/",
    GitHubApiBaseUrl = builder.Configuration["GitHub:ApiBaseUrl"] ?? "https://api.github.com/",
    GitHubToken = builder.Configuration["GitHub:Token"],
});

builder.Services.AddOutletCrmWeb();
builder.Services.AddHostedService<Outlet.Crm.Web.Workers.SnapshotCaptureWorker>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (useSqlite)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapOutletCrm();

app.Run();

/// <summary>Exposed so integration tests can spin the host via WebApplicationFactory.</summary>
public partial class Program;
