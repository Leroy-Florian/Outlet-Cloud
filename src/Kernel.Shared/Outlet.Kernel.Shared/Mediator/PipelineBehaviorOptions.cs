namespace Outlet.Kernel.Shared.Mediator;

/// <summary>
/// Configuration for built-in pipeline behaviors.
/// </summary>
public sealed class PipelineBehaviorOptions
{
    /// <summary>
    /// Use cases exceeding this duration emit a Warning log via SlowExecutionBehavior.
    /// Default: 500 ms.
    /// </summary>
    public TimeSpan SlowExecutionThreshold { get; set; } = TimeSpan.FromMilliseconds(500);
}
