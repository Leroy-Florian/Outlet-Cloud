namespace Outlet.Kernel.Shared;

/// <summary>
/// Boundary helper that converts an <see cref="ArgumentException"/> thrown by a
/// value-object factory (e.g. <c>RegistryItemId.From(...)</c>) into a
/// <see cref="Result{T}"/> carrying a domain error message. Used at the CLI/API
/// boundary so that use cases can drop their try/catch and receive
/// already-validated value objects.
/// </summary>
public static class Guard
{
    public static Result<T> TryBuild<T>(Func<T> factory, string onInvalid)
    {
        try
        {
            return Result<T>.Success(factory());
        }
        catch (ArgumentException)
        {
            return Result<T>.Failure(onInvalid);
        }
    }
}
