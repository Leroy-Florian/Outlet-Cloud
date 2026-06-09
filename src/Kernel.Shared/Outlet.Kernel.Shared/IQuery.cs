namespace Outlet.Kernel.Shared;

/// <summary>
/// Base interface for all queries following CQRS pattern.
/// Queries are read-only operations that return data without side effects.
/// Returns Result&lt;T&gt; for explicit success/failure handling.
/// </summary>
/// <typeparam name="TRequest">The query request type</typeparam>
/// <typeparam name="TResult">The success data type (wrapped in Result)</typeparam>
public interface IQuery<in TRequest, TResult>
{
    Task<Result<TResult>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
