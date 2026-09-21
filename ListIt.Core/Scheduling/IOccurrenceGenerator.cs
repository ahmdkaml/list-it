using System;
using System.Collections.Generic;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Defines the contract for generating concrete TaskOccurrence instances from task schedules.
/// Answers: "Given this task and this date/range, what occurrences should exist?"
/// Pure deterministic transformation without timers, system clock reads, or status evaluation.
/// </summary>
public interface IOccurrenceGenerator
{
    IReadOnlyList<TaskOccurrence> Generate(TaskBase task, DateOnly date);
    IReadOnlyList<TaskOccurrence> Generate(TaskBase task, DateOnly startDate, DateOnly endDate);
}
