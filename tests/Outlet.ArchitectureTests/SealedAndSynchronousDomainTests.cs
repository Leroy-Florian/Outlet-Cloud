using FluentAssertions;
using Outlet.Kernel.Shared;

namespace Outlet.ArchitectureTests;

public sealed class SealedAndSynchronousDomainTests : ArchitectureTestBase
{
    [Fact]
    public void Should_BeSealed_When_TypeInheritsFromValueObject()
    {
        var violations = AllDomainAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(ValueObject).IsAssignableFrom(t)
                && t != typeof(ValueObject))
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Value Objects must be sealed (CLAUDE.md: 'Use sealed on all concrete classes by default'):{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_BeSealed_When_TypeInheritsFromException_InDomain()
    {
        var violations = AllDomainAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(Exception).IsAssignableFrom(t)
                && t.Namespace?.Contains(".Domain") == true)
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Domain exceptions must be sealed (CLAUDE.md: 'ALL exceptions are sealed'):{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_NotExposeAsyncMethods_When_TypeIsInDomain()
    {
        var violations = AllDomainAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Namespace?.Contains(".Domain") == true
                && !t.Name.Contains("AssemblyReference"))
            .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Public
                                          | System.Reflection.BindingFlags.Instance
                                          | System.Reflection.BindingFlags.Static
                                          | System.Reflection.BindingFlags.DeclaredOnly))
            .Where(m => IsAsyncOrTaskReturning(m.ReturnType))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToList();

        violations.Should().BeEmpty(
            $"Domain methods must be synchronous (CLAUDE.md: 'NO async methods - domain is synchronous'):{Environment.NewLine}{FormatTypeList(violations)}");
    }

    private static bool IsAsyncOrTaskReturning(Type returnType)
    {
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            return true;

        if (returnType.IsGenericType)
        {
            var def = returnType.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(ValueTask<>))
                return true;
        }

        return false;
    }
}
