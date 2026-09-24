using System;
using System.Collections.Generic;
using ListIt.Core.Models;
using ListIt.Core.Repositories;

namespace ListIt.Core.Services;

/// <summary>
/// Orchestrates task CRUD operations and coordinates application-level validation with domain models and persistence.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<ListitTask> GetAllTasks()
    {
        return _repository.GetAll();
    }

    public ListitTask? GetTask(Guid id)
    {
        return _repository.GetById(id);
    }

    public ListitTask CreateTask(
        string title,
        TaskType type = TaskType.Recurring,
        TimeSpan? interval = null,
        string description = "",
        int urgency = 1,
        DateTime? startTime = null,
        int requiredCompletions = 1,
        bool bypassPrioritySuppression = false)
    {
        var task = new ListitTask(
            title: title,
            type: type,
            interval: interval,
            description: description,
            urgency: urgency,
            startTime: startTime,
            requiredCompletions: requiredCompletions,
            bypassPrioritySuppression: bypassPrioritySuppression);

        _repository.Add(task);
        return task;
    }

    public void UpdateTask(ListitTask task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        var existing = _repository.GetById(task.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Task with ID '{task.Id}' was not found.");
        }

        _repository.Update(task);
    }

    public bool DeleteTask(Guid id)
    {
        return _repository.Delete(id);
    }
}
