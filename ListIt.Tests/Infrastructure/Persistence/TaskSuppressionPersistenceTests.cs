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
        var recurring = new RecurringTask("Default Task", new[] { new TimeOnly(9, 0) });
        var finite = new FiniteTask("Default Finite", requiredCompletions: 1);

        // Assert
        Assert.False(recurring.BypassPrioritySuppression);
        Assert.False(finite.BypassPrioritySuppression);
    }

    [Fact]
    public void SaveAndReload_WithBypassPrioritySuppressionTrue_PreservesTrue()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var recurring = new RecurringTask(
            "Urgent Alerts",
            new[] { new TimeOnly(10, 0) },
            "Bypass task",
            urgency: 4,
            bypassPrioritySuppression: true);

        var finite = new FiniteTask(
            "Urgent Deliverable",
            requiredCompletions: 2,
            currentCompletions: 0,
            description: "Finite bypass",
            urgency: 3,
            dueAt: DateTime.UtcNow.AddDays(2),
            bypassPrioritySuppression: true);

        repo.Add(recurring);
        repo.Add(finite);

        // Act - Reload from disk using new repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loadedRecurring = reloadedRepo.GetById(recurring.Id) as RecurringTask;
        var loadedFinite = reloadedRepo.GetById(finite.Id) as FiniteTask;

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
        var recurring = new RecurringTask(
            "Normal Task",
            new[] { new TimeOnly(12, 0) },
            "Normal task",
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
