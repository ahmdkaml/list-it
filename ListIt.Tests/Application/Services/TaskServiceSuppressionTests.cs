using System;
using System.IO;
using ListIt.Core.Models;
using ListIt.Core.Services;
using ListIt.Infrastructure.Persistence;
using Xunit;

namespace ListIt.Tests.Application.Services;

public class TaskServiceSuppressionTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly JsonTaskRepository _repository;
    private readonly TaskService _service;

    public TaskServiceSuppressionTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"listit_service_suppression_{Guid.NewGuid():N}.json");
        _repository = new JsonTaskRepository(_tempFilePath);
        _service = new TaskService(_repository);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void CreateRecurringTask_WithBypassTrue_SetsBypassPrioritySuppressionTrue()
    {
        // Act
        var task = _service.CreateTask(
            title: "VIP Task",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "Priority bypass",
            urgency: 5,
            bypassPrioritySuppression: true);

        // Assert
        Assert.True(task.BypassPrioritySuppression);

        var retrieved = _service.GetTask(task.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.BypassPrioritySuppression);
    }

    [Fact]
    public void CreateFiniteTask_WithBypassTrue_SetsBypassPrioritySuppressionTrue()
    {
        // Act
        var task = _service.CreateTask(
            title: "Critical Milestone",
            type: TaskType.Finite,
            interval: TimeSpan.FromDays(1),
            description: "Must not be suppressed",
            urgency: 6,
            requiredCompletions: 1,
            bypassPrioritySuppression: true);

        // Assert
        Assert.True(task.BypassPrioritySuppression);

        var retrieved = _service.GetTask(task.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.BypassPrioritySuppression);
    }

    [Fact]
    public void UpdateTask_CanModifyBypassPrioritySuppression()
    {
        // Arrange
        var task = _service.CreateTask(
            title: "Flexible Task",
            type: TaskType.Recurring,
            bypassPrioritySuppression: false);

        Assert.False(task.BypassPrioritySuppression);

        // Act - Enable bypass
        task.SetBypassPrioritySuppression(true);
        _service.UpdateTask(task);

        // Assert
        var updated = _service.GetTask(task.Id);
        Assert.NotNull(updated);
        Assert.True(updated.BypassPrioritySuppression);

        // Act - Disable bypass
        task.SetBypassPrioritySuppression(false);
        _service.UpdateTask(task);

        // Assert
        var updatedAgain = _service.GetTask(task.Id);
        Assert.NotNull(updatedAgain);
        Assert.False(updatedAgain.BypassPrioritySuppression);
    }
}
