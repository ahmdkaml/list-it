using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListIt.Core.Notifications;
using ListIt.Core.Time;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Continuous application-level scheduler runtime.
/// Periodically evaluates scheduled task occurrences against current time and passes
/// notification decisions to INotificationPresenter.
/// </summary>
public class SchedulerRuntime : ISchedulerRuntime
{
    private readonly ISchedulingRuntime _schedulingRuntime;
    private readonly INotificationEngine _notificationEngine;
    private readonly INotificationPresenter _presenter;
    private readonly IClock _clock;
    private readonly SchedulerRuntimeOptions _options;

    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _cycleLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _isRunning;

    public bool IsRunning
    {
        get
        {
            lock (_stateLock)
            {
                return _isRunning;
            }
        }
    }

    public SchedulerRuntime(
        ISchedulingRuntime schedulingRuntime,
        INotificationEngine notificationEngine,
        INotificationPresenter notificationPresenter,
        IClock? clock = null,
        SchedulerRuntimeOptions? options = null)
    {
        _schedulingRuntime = schedulingRuntime ?? throw new ArgumentNullException(nameof(schedulingRuntime));
        _notificationEngine = notificationEngine ?? throw new ArgumentNullException(nameof(notificationEngine));
        _presenter = notificationPresenter ?? throw new ArgumentNullException(nameof(notificationPresenter));
        _clock = clock ?? SystemClock.Instance;
        _options = options ?? new SchedulerRuntimeOptions();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (_isRunning)
            {
                return Task.CompletedTask;
            }

            _isRunning = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
            return Task.CompletedTask;
        }
    }

    public async Task StopAsync()
    {
        Task? taskToWait = null;
        CancellationTokenSource? ctsToDispose = null;

        lock (_stateLock)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _cts?.Cancel();
            taskToWait = _loopTask;
            ctsToDispose = _cts;
            _loopTask = null;
            _cts = null;
        }

        if (taskToWait != null)
        {
            try
            {
                await taskToWait;
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception during scheduler loop shutdown: {ex}");
            }
        }

        ctsToDispose?.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        // 1. Initial immediate evaluation
        try
        {
            await EvaluateCycleAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Exception during initial scheduler runtime evaluation: {ex}");
        }

        // 2. Periodic evaluation loop
        using var timer = new PeriodicTimer(_options.EvaluationInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                {
                    break;
                }

                await EvaluateCycleAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception during scheduler runtime cycle: {ex}");
            }
        }
    }

    /// <summary>
    /// Evaluates a single scheduling cycle against the clock and forwards notification decisions.
    /// Thread-safe and protected against overlapping concurrent executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for this cycle.</param>
    /// <returns>Number of notifications emitted to the presenter.</returns>
    public async Task<int> EvaluateCycleAsync(CancellationToken cancellationToken = default)
    {
        await _cycleLock.WaitAsync(cancellationToken);
        try
        {
            var now = _clock.Now;
            var evaluations = _schedulingRuntime.EvaluateNow(now);

            // Active working task across the system (if any)
            var activeWorkingTask = evaluations.FirstOrDefault(e => e.Occurrence.IsWorking)?.Task;

            int notificationsEmitted = 0;

            foreach (var eval in evaluations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var context = new NotificationContext(
                        eval.Task,
                        eval.Occurrence,
                        now,
                        eval.State,
                        skipCount: 0,
                        activeWorkingTask: activeWorkingTask);

                    var decision = _notificationEngine.Evaluate(context);

                    if (decision.ShouldNotify)
                    {
                        await _presenter.PresentAsync(decision);
                        notificationsEmitted++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Exception isolation: Failure on one task must not block other tasks
                    Trace.TraceError($"Failed to evaluate notification for task {eval.Task.Id}: {ex}");
                }
            }

            return notificationsEmitted;
        }
        finally
        {
            _cycleLock.Release();
        }
    }
}
