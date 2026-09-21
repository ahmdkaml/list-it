using System;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;

namespace ListIt.Core.Notifications;

/// <summary>
/// Information context representing all data required to evaluate a notification decision.
/// Pure deterministic data container without calculating policy decisions.
/// </summary>
public class NotificationContext
{
    public TaskBase Task { get; }
    public TaskOccurrence Occurrence { get; }
    public DateTime CurrentTime { get; }
    public SchedulingState SchedulingState { get; }
    public int SkipCount { get; }
    public TaskBase? ActiveWorkingTask { get; }

    public Guid TaskId => Task.Id;
    public Guid OccurrenceId => Occurrence.OccurrenceId;
    public DateTime ScheduledAt => Occurrence.ScheduledAt;
    public int Urgency => Task.Urgency;
    public bool BypassPrioritySuppression => Task.BypassPrioritySuppression;
    public bool IsOccurrenceWorking => Occurrence.IsWorking;
    public bool HasActiveWorkingTask => ActiveWorkingTask != null;
    public int? ActiveWorkingTaskUrgency => ActiveWorkingTask?.Urgency;

    public NotificationContext(
        TaskBase task,
        TaskOccurrence occurrence,
        DateTime currentTime,
        SchedulingState schedulingState,
        int skipCount = 0,
        TaskBase? activeWorkingTask = null)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
        Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));

        if (occurrence.TaskId != task.Id)
        {
            throw new ArgumentException("Occurrence TaskId does not match the provided task.", nameof(occurrence));
        }

        if (skipCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skipCount), skipCount, "Skip count cannot be negative.");
        }

        CurrentTime = currentTime;
        SchedulingState = schedulingState;
        SkipCount = skipCount;
        ActiveWorkingTask = activeWorkingTask;
    }
}
