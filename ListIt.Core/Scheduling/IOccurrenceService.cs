using System;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Application service contract for coordinating occurrence lifecycle, active work,
/// and task completion counting.
/// </summary>
public interface IOccurrenceService
{
    void StartWorking(TaskOccurrence occurrence);
    void StopWorking(TaskOccurrence occurrence);
    void CompleteOccurrence(TaskOccurrence occurrence, TaskBase task);
    void MarkOccurrenceMissed(TaskOccurrence occurrence);
}
