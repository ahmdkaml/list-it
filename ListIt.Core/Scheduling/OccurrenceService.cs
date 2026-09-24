using System;
using ListIt.Core.Models;
using ListIt.Core.Services;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Coordinates occurrence lifecycle transitions, active-work state, and task completion counting.
/// Protects against double-completion and ensures finite-task completion invariants.
/// </summary>
public class OccurrenceService : IOccurrenceService
{
    private readonly ITaskService? _taskService;

    public OccurrenceService(ITaskService? taskService = null)
    {
        _taskService = taskService;
    }

    public void StartWorking(TaskOccurrence occurrence)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        occurrence.StartWorking();
    }

    public void StopWorking(TaskOccurrence occurrence)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        occurrence.StopWorking();
    }

    public void MarkOccurrenceMissed(TaskOccurrence occurrence, ListitTask? task = null)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        occurrence.MarkMissed();

        if (task != null)
        {
            task.RecordPass();
            _taskService?.UpdateTask(task);
        }
    }

    public void CompleteOccurrence(TaskOccurrence occurrence, ListitTask task)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
        if (task == null) throw new ArgumentNullException(nameof(task));

        if (occurrence.TaskId != task.Id)
        {
            throw new ArgumentException($"Occurrence task ID '{occurrence.TaskId}' does not match task ID '{task.Id}'.", nameof(occurrence));
        }

        // 1. Complete the occurrence (enforces Pending state, rejects if already Completed or Missed)
        occurrence.Complete();

        // 2. If it is a finite task, record completion and delete if target reached
        if (task.Type == TaskType.Finite)
        {
            task.RecordCompletion();

            // 3. Delegate application-level completion policy if task service is present
            if (_taskService != null)
            {
                if (task.CurrentCompletions >= task.RequiredCompletions)
                {
                    _taskService.DeleteTask(task.Id);
                }
                else
                {
                    task.ResetInterval(occurrence.ScheduledAt);
                    _taskService.UpdateTask(task);
                }
            }
        }
        else
        {
            // Recurring task: records completion, resets interval, and persists without deleting
            task.RecordCompletion();
            task.ResetInterval(occurrence.ScheduledAt);
            _taskService?.UpdateTask(task);
        }
    }
}
