using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Outlet.Kernel.Shared.Mediator;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var useCase = typeof(TRequest).Name;
        logger.LogInformation("Executing {UseCase}", useCase);

        var stopwatch = Stopwatch.StartNew();
        TResponse response;
        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Use case {UseCase} threw {ExceptionType} after {ElapsedMs}ms",
                useCase, ex.GetType().Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
        stopwatch.Stop();

        if (response is IResultStatus { IsFailure: true } failed)
        {
            logger.LogWarning(
                "Executed {UseCase} in {ElapsedMs}ms — failed with {Error}",
                useCase, stopwatch.ElapsedMilliseconds, failed.Error);
        }
        else
        {
            logger.LogInformation(
                "Executed {UseCase} in {ElapsedMs}ms",
                useCase, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
