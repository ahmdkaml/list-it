using System.Threading;
using System.Threading.Tasks;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Continuous application runtime system that periodically evaluates scheduled task occurrences
/// and passes notification decisions to INotificationPresenter.
/// </summary>
public interface ISchedulerRuntime
{
    /// <summary>
    /// Indicates whether the runtime evaluation loop is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the centralized evaluation loop asynchronously.
    /// Performs an immediate evaluation before periodic intervals begin.
    /// Idempotent: repeated calls while running are safe no-ops.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the centralized evaluation loop and waits for any running cycle to complete.
    /// Idempotent: repeated calls while stopped are safe no-ops.
    /// </summary>
    Task StopAsync();
}
