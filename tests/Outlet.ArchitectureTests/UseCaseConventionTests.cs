using FluentAssertions;
using Outlet.Kernel.Shared;

namespace Outlet.ArchitectureTests;

public sealed class UseCaseConventionTests : ArchitectureTestBase
{
    [Fact]
    public void Should_ReturnResult_When_TypeIsUseCase()
    {
        var useCases = GivenAllTypesInApplication()
            .Where(IsUseCase);

        var violations = CheckReturnType(useCases).ToList();

        violations.Should().BeEmpty(
            $"Use Cases should return Result or Result<T>:{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_ImplementIUseCase_When_TypeIsUseCase()
    {
        var useCases = GivenAllTypesInApplication()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("UseCase"));

        var violations = useCases
            .Where(t => !ImplementsIUseCase(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            $"Use Cases must implement IUseCase<TCommand, TResult> or IUseCase<TCommand>:{Environment.NewLine}{FormatTypeList(violations)}");
    }

    [Fact]
    public void Should_BeImmutable_When_TypeIsCommandOrQuery()
    {
        var commands = GivenAllTypesInApplication()
            .Where(t => t.IsClass && !t.IsAbstract &&
                       (t.Name.EndsWith("Command") || t.Name.EndsWith("Query")));

        var violations = commands
            .Where(HasPublicSetters)
            .Select(t => $"{t.FullName} has public setters")
            .ToList();

        violations.Should().BeEmpty(
            $"Commands and queries should be immutable (CQRS principle):{Environment.NewLine}{FormatTypeList(violations)}");
    }

    private static bool IsUseCase(Type type) =>
        type.IsClass &&
        !type.IsAbstract &&
        !ImplementsIQuery(type) &&
        (type.Name.EndsWith("UseCase") ||
         type.GetInterfaces().Any(i =>
             i.IsGenericType &&
             (i.GetGenericTypeDefinition() == typeof(IUseCase<,>) ||
              i.GetGenericTypeDefinition() == typeof(IUseCase<>))));

    private static bool ImplementsIUseCase(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            (i.GetGenericTypeDefinition() == typeof(IUseCase<,>) ||
             i.GetGenericTypeDefinition() == typeof(IUseCase<>)));

    private static bool ImplementsIQuery(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IQuery<,>));

    private static List<string> CheckReturnType(IEnumerable<Type> useCases)
    {
        var violations = new List<string>();

        foreach (var useCase in useCases)
        {
            var handleMethod = useCase.GetMethods()
                .FirstOrDefault(m => m.Name is "HandleAsync" or "Handle");

            if (handleMethod is null)
            {
                violations.Add($"{useCase.FullName} does not have a Handle/HandleAsync method");
                continue;
            }

            var returnType = handleMethod.ReturnType;
            var isValidReturn = IsResultType(returnType) ||
                               (returnType.IsGenericType &&
                                returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                                IsResultType(returnType.GetGenericArguments()[0]));

            if (!isValidReturn)
            {
                violations.Add($"{useCase.FullName}.{handleMethod.Name} returns {returnType.Name} instead of Result");
            }
        }

        return violations;
    }

    private static bool IsResultType(Type type) =>
        type == typeof(Result) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>));
}
