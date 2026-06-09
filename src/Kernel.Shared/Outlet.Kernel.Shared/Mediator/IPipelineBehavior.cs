namespace Outlet.Kernel.Shared.Mediator;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Cross-cutting behavior that wraps use case execution. Behaviors are composed
/// in registration order — the first registered becomes the outermost layer of the
/// pipeline, the handler itself is innermost.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
