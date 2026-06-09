using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Kernel.Shared.Mediator;

/// <summary>
/// DI extensions for Mediator registration.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mediator plus the built-in pipeline behaviors (logging + slow-execution).
    /// Registration order = pipeline order, outermost first: LoggingBehavior wraps
    /// SlowExecutionBehavior wraps the handler.
    /// </summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        Action<PipelineBehaviorOptions>? configureOptions = null)
    {
        services.AddScoped<IMediator, Mediator>();

        var optionsBuilder = services.AddOptions<PipelineBehaviorOptions>();
        if (configureOptions is not null)
            optionsBuilder.Configure(configureOptions);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SlowExecutionBehavior<,>));

        return services;
    }

    /// <summary>
    /// Registers all IQuery and IUseCase handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        List<Type> types = [.. assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false })];

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                if (!@interface.IsGenericType)
                    continue;

                var genericDef = @interface.GetGenericTypeDefinition();

                // Register IQuery<,>, IUseCase<,> and IUseCase<>
                if (genericDef == typeof(IQuery<,>)
                    || genericDef == typeof(IUseCase<,>)
                    || genericDef == typeof(IUseCase<>))
                {
                    services.AddScoped(@interface, type);
                }
            }
        }
    }
}
