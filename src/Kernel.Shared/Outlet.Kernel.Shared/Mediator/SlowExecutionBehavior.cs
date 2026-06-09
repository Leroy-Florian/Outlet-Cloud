using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Outlet.Kernel.Shared.Mediator;

public sealed class SlowExecutionBehavior<TRequest, TResponse>(
    ILogger<SlowExecutionBehavior<TRequest, TResponse>> logger,
    IOptions<PipelineBehaviorOptions> options) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly TimeSpan _threshold = options.Value.SlowExecutionThreshold;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.Elapsed > _threshold)
        {
            logger.LogWarning(
                "Slow use case execution: {UseCase} took {ElapsedMs}ms (threshold {ThresholdMs}ms)",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, (long)_threshold.TotalMilliseconds);
        }

        return response;
    }
}
