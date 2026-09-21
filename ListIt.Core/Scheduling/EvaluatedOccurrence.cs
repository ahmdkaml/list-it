using System;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Immutable snapshot pairing a task occurrence with its evaluated scheduling state and parent task.
/// </summary>
public class EvaluatedOccurrence
{
    public TaskOccurrence Occurrence { get; }
    public SchedulingState State { get; }
    public TaskBase Task { get; }

    public EvaluatedOccurrence(TaskOccurrence occurrence, SchedulingState state, TaskBase task)
    {
        Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
        State = state;
        Task = task ?? throw new ArgumentNullException(nameof(task));
    }
}
