using System;

namespace ListIt.Core.Models;

/// <summary>
/// Represents one specific scheduled instance of a task.
/// Pure domain model representing state only, completely independent of schedulers, timers, or persistence.
/// </summary>
public class TaskOccurrence
{
    public Guid OccurrenceId { get; }
    public Guid TaskId { get; }
    public DateTime ScheduledAt { get; }
    public OccurrenceStatus Status { get; private set; }

    public TaskOccurrence(Guid taskId, DateTime scheduledAt)
        : this(Guid.NewGuid(), taskId, scheduledAt, OccurrenceStatus.Pending)
    {
    }

    public TaskOccurrence(Guid occurrenceId, Guid taskId, DateTime scheduledAt, OccurrenceStatus status = OccurrenceStatus.Pending)
    {
        if (occurrenceId == Guid.Empty)
        {
            throw new ArgumentException("Occurrence ID cannot be empty.", nameof(occurrenceId));
        }

        if (taskId == Guid.Empty)
        {
            throw new ArgumentException("Task ID cannot be empty.", nameof(taskId));
        }

        if (!Enum.IsDefined(typeof(OccurrenceStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid occurrence status.");
        }

        OccurrenceId = occurrenceId;
        TaskId = taskId;
        ScheduledAt = scheduledAt;
        Status = status;
    }

    public void Complete()
    {
        if (Status != OccurrenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot complete an occurrence with status '{Status}'. Only Pending occurrences can be completed.");
        }

        Status = OccurrenceStatus.Completed;
    }

    public void MarkMissed()
    {
        if (Status != OccurrenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark as missed an occurrence with status '{Status}'. Only Pending occurrences can be marked as missed.");
        }

        Status = OccurrenceStatus.Missed;
    }
}
