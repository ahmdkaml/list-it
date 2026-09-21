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
    IReadOnlyList<TaskBase> GetAllTasks();
    TaskBase? GetTask(Guid id);
    RecurringTask CreateRecurringTask(string title, IEnumerable<TimeOnly> assignedTimes, string description = "", int urgency = 1);
    FiniteTask CreateFiniteTask(string title, int requiredCompletions, string description = "", int urgency = 1, DateTime? dueAt = null);
    void UpdateTask(TaskBase task);
    bool DeleteTask(Guid id);
}
