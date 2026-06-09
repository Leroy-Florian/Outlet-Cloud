using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Outlet.Kernel.Shared.Mediator;

public sealed class Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger) : IMediator
{
    /// <inheritdoc />
    public Task<Result<TResult>> SendAsync<TRequest, TResult>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        var handler = serviceProvider.GetService<IQuery<TRequest, TResult>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for query type {typeof(TRequest).Name}. " +
                $"Register IQuery<{typeof(TRequest).Name}, {typeof(TResult).Name}> in DI container.");

        return RunPipelineAsync<TRequest, Result<TResult>>(
            request,
            handler.GetType().Name,
            () => handler.HandleAsync(request, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<TResult>> ExecuteAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : notnull
    {
        var handler = serviceProvider.GetService<IUseCase<TCommand, TResult>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command type {typeof(TCommand).Name}. " +
                $"Register IUseCase<{typeof(TCommand).Name}, {typeof(TResult).Name}> in DI container.");

        return RunPipelineAsync<TCommand, Result<TResult>>(
            command,
            handler.GetType().Name,
            () => handler.HandleAsync(command, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : notnull
    {
        var handler = serviceProvider.GetService<IUseCase<TCommand>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command type {typeof(TCommand).Name}. " +
                $"Register IUseCase<{typeof(TCommand).Name}> in DI container.");

        return RunPipelineAsync<TCommand, Result>(
            command,
            handler.GetType().Name,
            () => handler.HandleAsync(command, cancellationToken),
            cancellationToken);
    }

    private async Task<TResponse> RunPipelineAsync<TRequest, TResponse>(
        TRequest request,
        string useCaseName,
        RequestHandlerDelegate<TResponse> handlerDelegate,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        // Behaviors are returned in registration order; reverse-fold so the FIRST
        // registered ends up as the outermost layer (executes first / completes last).
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        RequestHandlerDelegate<TResponse> pipeline = handlerDelegate;
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(request, next, cancellationToken);
        }

        using var _ = logger.BeginScope(new { UseCase = useCaseName });
        return await pipeline();
    }
}
