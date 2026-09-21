using System;
using System.Collections.Generic;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Deterministic evaluation engine that determines the SchedulingState of occurrences relative to a supplied current time.
/// Pure evaluation without system clock dependencies, occurrence mutation, timers, or notifications.
/// </summary>
public class Scheduler : IScheduler
{
    public SchedulingState Evaluate(TaskOccurrence occurrence, DateTime currentTime)
    {
        if (occurrence == null)
        {
            throw new ArgumentNullException(nameof(occurrence));
        }

        // Domain lifecycle state takes precedence over temporal evaluation
        if (occurrence.Status == OccurrenceStatus.Completed)
        {
            return SchedulingState.Completed;
        }

        if (occurrence.Status == OccurrenceStatus.Missed)
        {
            return SchedulingState.Missed;
        }

        // For pending occurrences, evaluate against temporal boundaries
        if (currentTime < occurrence.ScheduledAt)
        {
            return SchedulingState.Upcoming;
        }

        if (currentTime == occurrence.ScheduledAt)
        {
            return SchedulingState.Due;
        }

        return SchedulingState.Overdue;
    }

    public IReadOnlyDictionary<TaskOccurrence, SchedulingState> Evaluate(IEnumerable<TaskOccurrence> occurrences, DateTime currentTime)
    {
        if (occurrences == null)
        {
            throw new ArgumentNullException(nameof(occurrences));
        }

        var results = new Dictionary<TaskOccurrence, SchedulingState>();
        foreach (var occurrence in occurrences)
        {
            results[occurrence] = Evaluate(occurrence, currentTime);
        }

        return results;
    }
}
