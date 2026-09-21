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
    public bool IsWorking { get; private set; }

    /// <summary>
    /// Deterministic logical key identifying the occurrence by parent task and scheduled time.
    /// Used by schedulers to recognize duplicate occurrences across regeneration passes.
    /// </summary>
    public string LogicalKey => GetLogicalKey(TaskId, ScheduledAt);

    public static string GetLogicalKey(Guid taskId, DateTime scheduledAt) =>
        $"{taskId:D}_{scheduledAt:yyyy-MM-ddTHH:mm:ss}";

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
        IsWorking = false;
    }

    public void StartWorking()
    {
        if (Status != OccurrenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start working on an occurrence with status '{Status}'. Only Pending occurrences can be worked on.");
        }

        IsWorking = true;
    }

    public void StopWorking()
    {
        if (Status != OccurrenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot stop working on an occurrence with status '{Status}'. Only Pending occurrences can be updated.");
        }

        IsWorking = false;
    }

    public void Complete()
    {
        if (Status != OccurrenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot complete an occurrence with status '{Status}'. Only Pending occurrences can be completed.");
        }

        Status = OccurrenceStatus.Completed;
        IsWorking = false;
    }

    public void MarkMissed()
    {
        if (Status != OccurrenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark as missed an occurrence with status '{Status}'. Only Pending occurrences can be marked as missed.");
        }

        Status = OccurrenceStatus.Missed;
        IsWorking = false;
    }
}
