using System;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Configuration options for the centralized SchedulerRuntime evaluation loop.
/// </summary>
public class SchedulerRuntimeOptions
{
    /// <summary>
    /// The interval between periodic evaluation cycles.
    /// Default: 10 seconds.
    /// </summary>
    public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromSeconds(10);
}
