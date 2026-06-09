using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>Maps a use case <see cref="Result"/> to a minimal-API <see cref="IResult"/>.</summary>
public static class HttpResultMapping
{
    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : Results.BadRequest(new { error = result.Error });

    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
}
