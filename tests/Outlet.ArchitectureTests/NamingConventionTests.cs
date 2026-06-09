using FluentAssertions;
using Outlet.Kernel.Shared;

namespace Outlet.ArchitectureTests;

public sealed class NamingConventionTests : ArchitectureTestBase
{
    [Fact]
    public void Should_EndWithUseCase_When_TypeImplementsIUseCase()
    {
        var useCases = GivenAllTypesInApplication()
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       !ImplementsIQuery(t) &&
                       t.GetInterfaces().Any(i =>
                           i.IsGenericType &&
                           (i.GetGenericTypeDefinition() == typeof(IUseCase<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IUseCase<>))));

        var violations = useCases
            .Where(t => !t.Name.EndsWith("UseCase"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Types implementing IUseCase should end with 'UseCase':{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_EndWithRepository_When_TypeIsRepositoryImplementation()
    {
        var repositories = GivenAllTypesInInfrastructure()
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       !t.Name.StartsWith("Fake") &&
                       t.GetInterfaces().Any(i => i.Name.Contains("Repository")));

        var violations = repositories
            .Where(t => !t.Name.EndsWith("Repository"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Repository implementations should end with 'Repository':{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_StartWithI_When_TypeIsRepositoryInterface()
    {
        var repositoryInterfaces = GivenAllTypesInApplication()
            .Concat(GivenAllTypesInDomain())
            .Where(t => t.IsInterface && t.Name.Contains("Repository"));

        var violations = repositoryInterfaces
            .Where(t => !t.Name.StartsWith('I'))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Repository interfaces should start with 'I':{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_EndWithException_When_TypeIsException()
    {
        var exceptions = GivenAllTypesInDomain()
            .Concat(GivenAllTypesInApplication())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Exception).IsAssignableFrom(t));

        var violations = exceptions
            .Where(t => !t.Name.EndsWith("Exception"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Exception types should end with 'Exception':{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_EndWithEvent_When_TypeIsDomainEvent()
    {
        var events = GivenAllTypesInDomain()
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       t.GetInterfaces().Any(i => i.Name.Contains("DomainEvent") || i.Name.Contains("IDomainEvent")));

        var violations = events
            .Where(t => !t.Name.EndsWith("Event"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Domain events should end with 'Event':{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_StartWithI_When_TypeIsInterface()
    {
        var interfaces = GivenAllTypesInApplication()
            .Concat(GivenAllTypesInDomain())
            .Concat(GivenAllTypesInInfrastructure())
            .Where(t => t.IsInterface);

        var violations = interfaces
            .Where(t => !t.Name.StartsWith('I'))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Interfaces should start with 'I':{Environment.NewLine}{FormatTypeList(violations)}");
    }

    private static bool ImplementsIQuery(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IQuery<,>));
}
