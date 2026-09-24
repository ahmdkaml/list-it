using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ListIt.Core.Models;
using ListIt.Core.Services;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Background scheduling runtime engine that periodically generates occurrences,
/// evaluates their scheduling state against the current clock, and manages occurrence lifecycle actions.
/// Isolated from WPF and presentation concerns.
/// </summary>
public class SchedulingRuntime : ISchedulingRuntime
{
    private readonly ITaskService _taskService;
    private readonly IOccurrenceGenerator _generator;
    private readonly IScheduler _scheduler;
    private readonly IOccurrenceService _occurrenceService;
    private readonly Func<DateTime> _clock;
    private readonly TimeSpan _pollInterval;

    private readonly object _lock = new();
    private readonly Dictionary<string, TaskOccurrence> _trackedOccurrences = new();
    private List<EvaluatedOccurrence> _currentEvaluations = new();
    private Timer? _timer;
    private bool _isRunning;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    public event EventHandler<IReadOnlyList<EvaluatedOccurrence>>? StateEvaluated;

    public IReadOnlyList<EvaluatedOccurrence> CurrentEvaluations
    {
        get
        {
            lock (_lock)
            {
                return _currentEvaluations.AsReadOnly();
            }
        }
    }

    public SchedulingRuntime(
        ITaskService taskService,
        IOccurrenceGenerator generator,
        IScheduler scheduler,
        IOccurrenceService occurrenceService,
        Func<DateTime>? clock = null,
        TimeSpan? pollInterval = null)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _occurrenceService = occurrenceService ?? throw new ArgumentNullException(nameof(occurrenceService));
        _clock = clock ?? (() => DateTime.UtcNow);
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(10);
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
            _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, _pollInterval);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void OnTimerTick(object? state)
    {
        try
        {
            EvaluateNow();
        }
        catch
        {
            // Suppress background tick exceptions to avoid crashing runtime loop
        }
    }

    public IReadOnlyList<EvaluatedOccurrence> EvaluateNow(DateTime? currentTime = null)
    {
        lock (_lock)
        {
            var now = currentTime ?? _clock();
            var currentDate = DateOnly.FromDateTime(now);
            var tasks = _taskService.GetAllTasks();
            var activeTaskIds = new HashSet<Guid>(tasks.Select(t => t.Id));

            // Clean up tracked occurrences for tasks that were deleted
            var deadKeys = _trackedOccurrences
                .Where(kvp => !activeTaskIds.Contains(kvp.Value.TaskId))
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in deadKeys)
            {
                _trackedOccurrences.Remove(key);
            }

            var evaluatedList = new List<EvaluatedOccurrence>();

            foreach (var task in tasks)
            {
                List<TaskOccurrence> occurrences = new();

                if (task.Type == TaskType.Recurring)
                {
                    var generated = _generator.Generate(task, currentDate);
                    foreach (var occ in generated)
                    {
                        if (_trackedOccurrences.TryGetValue(occ.LogicalKey, out var existing))
                        {
                            occurrences.Add(existing);
                        }
                        else
                        {
                            _trackedOccurrences[occ.LogicalKey] = occ;
                            occurrences.Add(occ);
                        }
                    }
                }
                else if (task.Type == TaskType.Finite)
                {
                    if (task.CurrentCompletions < task.RequiredCompletions)
                    {
                        // Stagger scheduledAt by CurrentCompletions seconds so each sequential completion has a unique LogicalKey
                        var scheduledAt = task.GetNextDeadlineUtc().AddSeconds(task.CurrentCompletions);
                        var key = TaskOccurrence.GetLogicalKey(task.Id, scheduledAt);

                        if (_trackedOccurrences.TryGetValue(key, out var existing))
                        {
                            occurrences.Add(existing);
                        }
                        else
                        {
                            var occ = new TaskOccurrence(task.Id, scheduledAt);
                            _trackedOccurrences[key] = occ;
                            occurrences.Add(occ);
                        }
                    }
                }

                foreach (var occ in occurrences)
                {
                    var state = _scheduler.Evaluate(occ, now);
                    evaluatedList.Add(new EvaluatedOccurrence(occ, state, task));
                }
            }

            _currentEvaluations = evaluatedList;
            StateEvaluated?.Invoke(this, _currentEvaluations.AsReadOnly());
            return _currentEvaluations.AsReadOnly();
        }
    }

    public void StartWorking(TaskOccurrence occurrence)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        lock (_lock)
        {
            _occurrenceService.StartWorking(occurrence);
            EvaluateNow();
        }
    }

    public void StopWorking(TaskOccurrence occurrence)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        lock (_lock)
        {
            _occurrenceService.StopWorking(occurrence);
            EvaluateNow();
        }
    }

    public void CompleteOccurrence(TaskOccurrence occurrence)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        lock (_lock)
        {
            var task = _taskService.GetTask(occurrence.TaskId);
            if (task == null)
            {
                throw new InvalidOperationException($"Parent task with ID '{occurrence.TaskId}' was not found.");
            }

            _occurrenceService.CompleteOccurrence(occurrence, task);
            EvaluateNow();
        }
    }

    public void MarkOccurrenceMissed(TaskOccurrence occurrence)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        lock (_lock)
        {
            _occurrenceService.MarkOccurrenceMissed(occurrence);
            EvaluateNow();
        }
    }

    public IReadOnlyList<EvaluatedOccurrence> GetOccurrencesForTask(Guid taskId)
    {
        lock (_lock)
        {
            return _currentEvaluations.Where(e => e.Task.Id == taskId).ToList().AsReadOnly();
        }
    }

    public TaskOccurrence? GetOccurrence(Guid occurrenceId)
    {
        lock (_lock)
        {
            return _trackedOccurrences.Values.FirstOrDefault(o => o.OccurrenceId == occurrenceId);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
