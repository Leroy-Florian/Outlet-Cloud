namespace Outlet.ArchitectureTests;

/// <summary>
/// Port purity: the hexagon (Domain + Application) must stay free of technical
/// dependencies. HTTP, JSON, Roslyn and MSBuild are Infrastructure concerns —
/// they only ever appear behind ports (IRegistryClient, INamespaceRewriter,
/// IProjectInspector, INuGetEditor).
/// </summary>
public sealed class TechnicalDependencyTests : ArchitectureTestBase
{
    private static readonly string[] ForbiddenHttpDependencies =
    [
        "System.Net.Http",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions.Http",
    ];

    private static readonly string[] ForbiddenJsonDependencies =
    [
        "System.Text.Json",
        "Newtonsoft.Json",
        "System.Text.Json.Serialization",
    ];

    private static readonly string[] ForbiddenDatabaseDependencies =
    [
        "Microsoft.EntityFrameworkCore",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "Npgsql",
        "MongoDB.Driver",
        "Dapper",
        "Microsoft.Azure.Cosmos",
    ];

    private static readonly string[] ForbiddenLoggingDependencies =
    [
        "Microsoft.Extensions.Logging",
        "Serilog",
        "NLog",
        "log4net",
    ];

    private static readonly string[] ForbiddenResilienceDependencies =
    [
        "Polly",
        "Microsoft.Extensions.Http.Resilience",
    ];

    // Roslyn is how the INamespaceRewriter ADAPTER works, never how the
    // hexagon thinks. Same for MSBuild evaluation (IProjectInspector adapter).
    private static readonly string[] ForbiddenCodeToolingDependencies =
    [
        "Microsoft.CodeAnalysis",
        "Microsoft.Build",
    ];

    #region Domain Layer Tests

    [Fact]
    public void Should_HaveNoDependency_When_DomainUsesHttp()
    {
        var types = GivenTypesInDomain();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenHttpDependencies);
        ThenShouldHaveNoViolations(result, "Domain", "HTTP");
    }

    [Fact]
    public void Should_HaveNoDependency_When_DomainUsesJson()
    {
        var types = GivenTypesInDomain();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenJsonDependencies);
        ThenShouldHaveNoViolations(result, "Domain", "JSON");
    }

    [Fact]
    public void Should_HaveNoDependency_When_DomainUsesDatabase()
    {
        var types = GivenTypesInDomain();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenDatabaseDependencies);
        ThenShouldHaveNoViolations(result, "Domain", "Database");
    }

    [Fact]
    public void Should_HaveNoDependency_When_DomainUsesLogging()
    {
        var types = GivenTypesInDomain();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenLoggingDependencies);
        ThenShouldHaveNoViolations(result, "Domain", "Logging");
    }

    [Fact]
    public void Should_HaveNoDependency_When_DomainUsesResilience()
    {
        var types = GivenTypesInDomain();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenResilienceDependencies);
        ThenShouldHaveNoViolations(result, "Domain", "Resilience");
    }

    [Fact]
    public void Should_HaveNoDependency_When_DomainUsesCodeTooling()
    {
        var types = GivenTypesInDomain();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenCodeToolingDependencies);
        ThenShouldHaveNoViolations(result, "Domain", "Roslyn/MSBuild");
    }

    #endregion

    #region Application Layer Tests

    [Fact]
    public void Should_HaveNoDependency_When_ApplicationUsesDatabase()
    {
        var types = GivenTypesInApplication();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenDatabaseDependencies);
        ThenShouldHaveNoViolations(result, "Application", "Database");
    }

    [Fact]
    public void Should_HaveNoDependency_When_ApplicationUsesHttp()
    {
        var types = GivenTypesInApplication();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenHttpDependencies);
        ThenShouldHaveNoViolations(result, "Application", "HTTP");
    }

    [Fact]
    public void Should_HaveNoDependency_When_ApplicationUsesJson()
    {
        var types = GivenTypesInApplication();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenJsonDependencies);
        ThenShouldHaveNoViolations(result, "Application", "JSON");
    }

    [Fact]
    public void Should_HaveNoDependency_When_ApplicationUsesLogging()
    {
        var types = GivenTypesInApplication();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenLoggingDependencies);
        ThenShouldHaveNoViolations(result, "Application", "Logging");
    }

    [Fact]
    public void Should_HaveNoDependency_When_ApplicationUsesResilience()
    {
        var types = GivenTypesInApplication();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenResilienceDependencies);
        ThenShouldHaveNoViolations(result, "Application", "Resilience");
    }

    [Fact]
    public void Should_HaveNoDependency_When_ApplicationUsesCodeTooling()
    {
        var types = GivenTypesInApplication();
        var result = WhenCheckingNoDependencyOn(types, ForbiddenCodeToolingDependencies);
        ThenShouldHaveNoViolations(result, "Application", "Roslyn/MSBuild");
    }

    #endregion
}
