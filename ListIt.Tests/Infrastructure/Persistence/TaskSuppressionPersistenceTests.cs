using System;
using System.IO;
using ListIt.Core.Models;
using ListIt.Infrastructure.Persistence;
using Xunit;

namespace ListIt.Tests.Infrastructure.Persistence;

public class TaskSuppressionPersistenceTests : IDisposable
{
    private readonly string _tempFilePath;

    public TaskSuppressionPersistenceTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"listit_suppression_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void NewlyCreatedTask_DefaultsToBypassPrioritySuppressionFalse()
    {
        // Arrange & Act
        var recurring = new ListitTask("Default Task", TaskType.Recurring);
        var finite = new ListitTask("Default Finite", TaskType.Finite, requiredCompletions: 1);

        // Assert
        Assert.False(recurring.BypassPrioritySuppression);
        Assert.False(finite.BypassPrioritySuppression);
    }

    [Fact]
    public void SaveAndReload_WithBypassPrioritySuppressionTrue_PreservesTrue()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var recurring = new ListitTask(
            title: "Urgent Alerts",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "Bypass task",
            urgency: 4,
            bypassPrioritySuppression: true);

        var finite = new ListitTask(
            title: "Urgent Deliverable",
            type: TaskType.Finite,
            interval: TimeSpan.FromDays(2),
            description: "Finite bypass",
            urgency: 3,
            requiredCompletions: 2,
            currentCompletions: 0,
            bypassPrioritySuppression: true);

        repo.Add(recurring);
        repo.Add(finite);

        // Act - Reload from disk using new repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loadedRecurring = reloadedRepo.GetById(recurring.Id);
        var loadedFinite = reloadedRepo.GetById(finite.Id);

        // Assert
        Assert.NotNull(loadedRecurring);
        Assert.True(loadedRecurring.BypassPrioritySuppression);

        Assert.NotNull(loadedFinite);
        Assert.True(loadedFinite.BypassPrioritySuppression);
    }

    [Fact]
    public void SaveAndReload_WithBypassPrioritySuppressionFalse_PreservesFalse()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var recurring = new ListitTask(
            title: "Normal Task",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "Normal task",
            urgency: 2,
            bypassPrioritySuppression: false);

        repo.Add(recurring);

        // Act - Reload from disk
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loaded = reloadedRepo.GetById(recurring.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.False(loaded.BypassPrioritySuppression);
    }

    [Fact]
    public void BackwardCompatibility_LegacyJsonWithoutBypassProperty_LoadsAsFalse()
    {
        // Arrange - JSON representing tasks before BypassPrioritySuppression was introduced
        var legacyJson = @"[
  {
    ""type"": ""recurring"",
    ""AssignedTimes"": [
      ""09:00:00""
    ],
    ""Id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
    ""Title"": ""Legacy Recurring"",
    ""Description"": ""Created in earlier version"",
    ""Urgency"": 2,
    ""CreatedAt"": ""2026-09-01T12:00:00Z""
  },
  {
    ""type"": ""finite"",
    ""RequiredCompletions"": 3,
    ""CurrentCompletions"": 1,
    ""DueAt"": ""2026-09-25T15:00:00Z"",
    ""Id"": ""7ba85f64-5717-4562-b3fc-2c963f66afa7"",
    ""Title"": ""Legacy Finite"",
    ""Description"": ""Legacy finite task"",
    ""Urgency"": 1,
    ""CreatedAt"": ""2026-09-01T12:00:00Z""
  }
]";
        File.WriteAllText(_tempFilePath, legacyJson);

        // Act
        var repo = new JsonTaskRepository(_tempFilePath);
        var tasks = repo.GetAll();

        // Assert
        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.False(t.BypassPrioritySuppression));
    }
}
