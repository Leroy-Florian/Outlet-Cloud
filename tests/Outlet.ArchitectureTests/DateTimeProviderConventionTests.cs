using System.Text.RegularExpressions;
using FluentAssertions;

namespace Outlet.ArchitectureTests;

/// <summary>
/// Enforces that no Domain or Application source file calls <c>DateTime.UtcNow</c>
/// or <c>DateTime.Now</c> directly. The single ambient bridge is
/// <see cref="Outlet.Kernel.Shared.UtcDateTimeProvider"/>, which all consumers reach
/// via <see cref="Outlet.Kernel.Shared.ICurrentDateTimeProvider"/>.
///
/// NetArchTest works on compiled IL dependencies, but <c>DateTime.UtcNow</c> is a
/// property access on <see cref="System.DateTime"/>. Banning <c>System</c> is not
/// realistic, so this test scans the source tree textually.
/// </summary>
public sealed class DateTimeProviderConventionTests
{
    private static readonly Regex ForbiddenPattern =
        new(@"\bDateTime\.(UtcNow|Now)\b", RegexOptions.Compiled);

    // Allow-list: the single ambient bridge plus its tests. Anything else must inject
    // ICurrentDateTimeProvider.
    private static readonly string[] AllowedFileSuffixes =
    [
        "Outlet.Kernel.Shared/UtcDateTimeProvider.cs",
        "Outlet.Kernel.Shared.UnitTests/UtcDateTimeProviderTests.cs",
    ];

    [Fact]
    public void Should_HaveNoDirectDateTimeUtcNow_When_FileIsInDomainOrApplication()
    {
        var repoRoot = LocateRepositoryRoot();
        var srcRoot = Path.Combine(repoRoot, "src");

        var offenders = Directory
            .EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(IsDomainOrApplicationSource)
            .Where(path => !IsAllowed(path))
            .SelectMany(FindOffendingLines)
            .ToList();

        offenders.Should().BeEmpty(
            "Domain and Application code must obtain the current time via " +
            "ICurrentDateTimeProvider (injected), not via DateTime.UtcNow / DateTime.Now. " +
            "Offending sites:\n" + string.Join("\n", offenders.Select(o => "  - " + o)));
    }

    private static bool IsDomainOrApplicationSource(string fullPath)
    {
        var normalized = fullPath.Replace('\\', '/');

        // Skip generated and build outputs.
        if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) return false;

        // Only fail on production sources — unit tests are allowed to time-travel via
        // hand-written FixedClock fakes.
        if (normalized.Contains(".UnitTests/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalized.Contains(".IntegrationTests/", StringComparison.OrdinalIgnoreCase)) return false;

        return normalized.Contains(".Domain/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(".Application/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowed(string fullPath)
    {
        var normalized = fullPath.Replace('\\', '/');
        return AllowedFileSuffixes.Any(suffix => normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> FindOffendingLines(string fullPath)
    {
        var lines = File.ReadAllLines(fullPath);
        for (var i = 0; i < lines.Length; i++)
        {
            if (ForbiddenPattern.IsMatch(lines[i]))
                yield return $"{fullPath}:{i + 1} -> {lines[i].Trim()}";
        }
    }

    private static string LocateRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 12 && current is not null; i++)
        {
            if (Directory.Exists(Path.Combine(current, "src"))
                && File.Exists(Path.Combine(current, "Outlet.Cloud.slnx")))
                return current;
            current = Path.GetDirectoryName(current);
        }

        throw new InvalidOperationException(
            $"Could not locate repository root (Outlet.Cloud.slnx) starting from {AppContext.BaseDirectory}.");
    }
}
