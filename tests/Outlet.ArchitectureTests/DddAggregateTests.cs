using System.Reflection;
using FluentAssertions;
using Outlet.Kernel.Shared;

namespace Outlet.ArchitectureTests;

public sealed class DddAggregateTests : ArchitectureTestBase
{
    private static IEnumerable<Type> GivenAggregateRootsInDomain() =>
        AllDomainAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && InheritsFromAggregateRoot(t));

    [Fact]
    public void Should_HavePrivateConstructor_When_TypeIsAggregateRoot()
    {
        var aggregateRoots = GivenAggregateRootsInDomain();

        var violations = aggregateRoots
            .Where(HasOnlyPublicConstructors)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Aggregate Roots should have private/protected constructors (use factory methods):{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_HaveCreateFactoryMethod_When_TypeIsAggregateRoot()
    {
        var aggregateRoots = GivenAggregateRootsInDomain();

        var violations = aggregateRoots
            .Where(t => !HasCreateFactoryMethod(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Aggregate Roots should have a static 'Create' factory method:{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_NotHavePublicParameterlessConstructor_When_TypeIsAggregateRoot()
    {
        var aggregateRoots = GivenAggregateRootsInDomain();

        var violations = aggregateRoots
            .Where(HasPublicParameterlessConstructor)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Aggregate Roots should not have public parameterless constructors:{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_ReturnSelfOrResult_When_CreateMethodExists()
    {
        var aggregateRoots = GivenAggregateRootsInDomain();

        var violations = aggregateRoots
            .Where(t => HasCreateFactoryMethod(t) && !CreateMethodReturnsCorrectType(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Aggregate Root 'Create' method should return the aggregate type or Result<Aggregate>:{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Entities_ShouldNot_ReferenceOtherAggregateRoots()
    {
        var violatingTypes = new List<string>();

        foreach (var assembly in AllDomainAssemblies)
        {
            var aggregateRootTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && InheritsFromAggregateRoot(t))
                .ToList();

            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass
                    && !t.IsAbstract
                    && t.Namespace?.Contains(".Domain") == true
                    && !InheritsFromAggregateRoot(t)
                    && !t.Name.EndsWith("Id")
                    && !t.Name.Contains("Event"))
                .ToList();

            foreach (var entity in entityTypes)
            {
                var properties = entity.GetProperties();

                foreach (var prop in properties)
                {
                    if (aggregateRootTypes.Any(ar => ar == prop.PropertyType ||
                        (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericArguments().Any(g => aggregateRootTypes.Contains(g)))))
                    {
                        violatingTypes.Add($"{entity.FullName}.{prop.Name} references aggregate root {prop.PropertyType.Name}");
                    }
                }
            }
        }

        Assert.Empty(violatingTypes);
    }

    #region Helper Methods

    private static bool HasOnlyPublicConstructors(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return constructors.Length > 0 && constructors.All(c => c.IsPublic);
    }

    private static bool HasPublicParameterlessConstructor(Type type) =>
        type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes) is not null;

    private static bool HasCreateFactoryMethod(Type type) =>
        type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Any(m => m.Name == "Create" || m.Name.StartsWith("Create"));

    private static bool CreateMethodReturnsCorrectType(Type type) =>
        type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == "Create" || m.Name.StartsWith("Create"))
            .Any(m =>
            {
                var returnType = m.ReturnType;
                if (returnType == type) return true;
                if (returnType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == typeof(Result<>) &&
                    returnType.GetGenericArguments()[0] == type)
                    return true;
                return false;
            });

    private static bool InheritsFromAggregateRoot(Type type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name.Contains("AggregateRoot"))
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    #endregion
}
