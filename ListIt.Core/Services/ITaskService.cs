using System;
using System.Collections.Generic;
using ListIt.Core.Models;

namespace ListIt.Core.Services;

/// <summary>
/// Application service abstraction for task CRUD operations.
/// Sits between presentation and persistence.
/// </summary>
public interface ITaskService
{
    IReadOnlyList<ListitTask> GetAllTasks();
    ListitTask? GetTask(Guid id);
    ListitTask CreateTask(
        string title,
        TaskType type = TaskType.Recurring,
        TimeSpan? interval = null,
        string description = "",
        int urgency = 1,
        DateTime? startTime = null,
        int requiredCompletions = 1,
        bool bypassPrioritySuppression = false);
    void UpdateTask(ListitTask task);
    bool DeleteTask(Guid id);
}
