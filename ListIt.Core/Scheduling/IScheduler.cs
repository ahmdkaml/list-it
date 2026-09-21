using System;
using System.Collections.Generic;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Evaluates TaskOccurrence instances against a supplied current time to determine their scheduling state.
/// Pure deterministic evaluation engine: does not read the system clock, does not run timers,
/// and does not send notifications.
/// </summary>
public interface IScheduler
{
    SchedulingState Evaluate(TaskOccurrence occurrence, DateTime currentTime);
    IReadOnlyDictionary<TaskOccurrence, SchedulingState> Evaluate(IEnumerable<TaskOccurrence> occurrences, DateTime currentTime);
}
