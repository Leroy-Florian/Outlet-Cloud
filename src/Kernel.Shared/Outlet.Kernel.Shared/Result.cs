namespace Outlet.Kernel.Shared;

/// <summary>
/// Common surface of Result and Result&lt;T&gt;, used by mediator pipeline behaviors
/// (logging, slow-execution) to inspect outcomes without reflection or dynamic dispatch.
/// </summary>
public interface IResultStatus
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    string? Error { get; }
}

/// <summary>
/// Generic Result pattern for use case outcomes.
/// Replaces scattered result types with a unified approach.
/// Business errors are carried as a plain error message/code — expected failures
/// never throw; exceptions are reserved for bugs and infrastructure faults.
/// </summary>
/// <typeparam name="T">The success value type</typeparam>
public sealed class Result<T> : IResultStatus
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(mapper(Value!))
            : Result<TNew>.Failure(Error!);
    }

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(await mapper(Value!))
            : Result<TNew>.Failure(Error!);
    }
}

/// <summary>
/// Non-generic Result for operations without return value.
/// </summary>
public sealed class Result : IResultStatus
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result Failure(string error) => new(false, error);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error!);
    }
}
