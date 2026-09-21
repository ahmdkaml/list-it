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

    public IReadOnlyList<TaskBase> GetAllTasks()
    {
        return _repository.GetAll();
    }

    public TaskBase? GetTask(Guid id)
    {
        return _repository.GetById(id);
    }

    public RecurringTask CreateRecurringTask(string title, IEnumerable<TimeOnly> assignedTimes, string description = "", int urgency = 1)
    {
        var task = new RecurringTask(title, assignedTimes, description, urgency);
        _repository.Add(task);
        return task;
    }

    public FiniteTask CreateFiniteTask(string title, int requiredCompletions, string description = "", int urgency = 1)
    {
        var task = new FiniteTask(title, requiredCompletions, currentCompletions: 0, description, urgency);
        _repository.Add(task);
        return task;
    }

    public void UpdateTask(TaskBase task)
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
