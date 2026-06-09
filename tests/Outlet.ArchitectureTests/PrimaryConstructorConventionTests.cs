using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Outlet.ArchitectureTests;

/// <summary>
/// Enforces the rule documented in CLAUDE.md: every class and struct OUTSIDE the Domain
/// layer that needs constructor parameters MUST declare them as a C# 12+ primary constructor
/// on the type declaration itself, instead of an explicit constructor body.
///
/// Records are exempt (they already use primary-constructor syntax by design).
/// Private constructors (factory-only construction) are allowed, since primary-constructor
/// accessibility always matches the type accessibility and cannot be made private.
/// </summary>
public sealed class PrimaryConstructorConventionTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    private static readonly string[] ScopedRoots =
    [
        "src",
    ];

    private static readonly string[] ExcludedPathFragments =
    [
        "/bin/",
        "/obj/",
        ".Domain/",
        "AssemblyReference.cs",
    ];

    // Matches an explicit PARAMETERIZED constructor declaration line inside a type body.
    // Example: "    public Foo(int x)" — requires at least one character between parens
    // so empty `Foo()` ctors (test-composition glue) are not flagged.
    // Excludes class/struct/record declarations.
    private static readonly Regex CtorPattern = new(
        @"^\s*(public|internal|protected)(\s+(static|sealed|abstract|partial|override|virtual|new|unsafe))*\s+(?<name>[A-Z][A-Za-z0-9_]*)\s*\(\s*[^\s)]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Matches a type declaration: "public sealed class Foo" / "internal struct Bar" / "record Baz"
    private static readonly Regex TypeDeclPattern = new(
        @"^\s*(?:(?:public|internal|protected|private)\s+)?(?:(?:static|sealed|abstract|partial|readonly|ref)\s+)*\b(?<kind>class|struct|record)\b\s+(?<name>[A-Z][A-Za-z0-9_]*)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void All_non_domain_classes_and_structs_should_use_primary_constructors()
    {
        var violations = new List<string>();

        foreach (var root in ScopedRoots)
        {
            var fullRoot = Path.Combine(RepoRoot, root);
            if (!Directory.Exists(fullRoot)) continue;

            foreach (var file in Directory.EnumerateFiles(fullRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsExcluded(file)) continue;

                var source = File.ReadAllText(file);
                var fileViolations = FindExplicitConstructors(source);
                foreach (var v in fileViolations)
                {
                    violations.Add($"{Path.GetRelativePath(RepoRoot, file)} → '{v}' constructor");
                }
            }
        }

        violations.Should().BeEmpty(
            "All classes/structs outside Domain must declare parameters via a primary constructor on the type declaration (see CLAUDE.md → 'Primary Constructors'). " +
            $"Found {violations.Count} violation(s):{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    private static IEnumerable<string> FindExplicitConstructors(string source)
    {
        // We work line-by-line on the ORIGINAL source so the `// non-primary:` opt-out
        // marker (which only exists in comments) stays visible.
        var lines = source.Split('\n');

        // First pass: collect every class/struct/record name and remember which ones
        // are records (records already use primary-constructor syntax by design and
        // are not in scope).
        var typeNames = new HashSet<string>(StringComparer.Ordinal);
        var recordNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            var m = TypeDeclPattern.Match(lines[i]);
            if (!m.Success) continue;
            var name = m.Groups["name"].Value;
            typeNames.Add(name);
            if (m.Groups["kind"].Value == "record") recordNames.Add(name);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip lines that contain a type declaration (class/struct/record on same line):
            // the ctor regex would otherwise re-match the type name immediately after the
            // type keyword (primary-ctor form).
            if (Regex.IsMatch(line, @"\b(class|struct|record)\b\s+[A-Z]")) continue;

            // Strip line/block comments and string literals so a class name appearing in a
            // comment or string isn't read as a ctor.
            var stripped = StripCommentsAndStrings(line);
            var m = CtorPattern.Match(stripped);
            if (!m.Success) continue;

            var name = m.Groups["name"].Value;
            if (!typeNames.Contains(name)) continue;
            if (recordNames.Contains(name)) continue;

            // Exempt purely-private/protected constructors used by factory patterns:
            // a primary constructor cannot be made narrower than the type's accessibility,
            // so `private Foo(...)` inside a public class is the only way to gate construction
            // through a factory method.
            var modifier = m.Value.TrimStart().Split(' ', 2)[0];
            if (modifier == "private" || modifier == "protected") continue;

            // Explicit opt-out: `// non-primary: <reason>` in the 6 lines preceding the ctor.
            // Used for ctors that subscribe to events, call instance methods, or have init
            // ordering that field initializers cannot express.
            if (HasNonPrimaryOptOut(lines, i)) continue;

            if (seen.Add(name + "@" + i))
                yield return name;
        }
    }

    private static bool HasNonPrimaryOptOut(string[] lines, int ctorLineIndex)
    {
        var start = Math.Max(0, ctorLineIndex - 6);
        for (var i = start; i < ctorLineIndex; i++)
        {
            if (lines[i].Contains("// non-primary", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string StripCommentsAndStrings(string source)
    {
        // Remove single-line comments
        var s = Regex.Replace(source, @"//[^\r\n]*", "");
        // Remove multi-line comments
        s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
        // Remove string literals (best effort — sufficient for ctor detection)
        s = Regex.Replace(s, "\"(?:\\\\.|[^\"\\\\])*\"", "\"\"");
        s = Regex.Replace(s, "@\"(?:\"\"|[^\"])*\"", "\"\"");
        return s;
    }

    private static bool IsExcluded(string path)
    {
        var normalized = path.Replace('\\', '/');
        return ExcludedPathFragments.Any(frag => normalized.Contains(frag, StringComparison.OrdinalIgnoreCase));
    }

    private static string LocateRepoRoot([CallerFilePath] string callerPath = "")
    {
        // callerPath = .../tests/Outlet.ArchitectureTests/PrimaryConstructorConventionTests.cs
        // repo root = two levels up.
        var dir = Path.GetDirectoryName(callerPath)!;
        for (var i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "src")) && File.Exists(Path.Combine(dir, "Outlet.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir)!;
            if (string.IsNullOrEmpty(dir)) break;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from {callerPath}. Expected to find a directory with 'src/' and 'Outlet.slnx'.");
    }
}
