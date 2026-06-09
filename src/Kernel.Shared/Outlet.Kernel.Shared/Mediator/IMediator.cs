namespace Outlet.Kernel.Shared.Mediator;

public interface IMediator
{
    Task<Result<TResult>> SendAsync<TRequest, TResult>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;

    Task<Result<TResult>> ExecuteAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : notnull;

    Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : notnull;
}
