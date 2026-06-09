namespace Outlet.Kernel.Shared;

/// <summary>
/// Base interface for all use cases following CQRS pattern.
/// Similar to MediatR IRequestHandler but simpler and domain-focused.
/// Returns Result&lt;T&gt; for explicit success/failure handling.
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResult">The success data type (wrapped in Result)</typeparam>
public interface IUseCase<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Use case without result data (void equivalent)
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public interface IUseCase<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
