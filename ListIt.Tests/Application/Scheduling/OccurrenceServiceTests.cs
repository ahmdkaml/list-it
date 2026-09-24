using System;
using System.Collections.Generic;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using Xunit;

namespace ListIt.Tests.Application.Scheduling;

public class OccurrenceServiceTests
{
    private class FakeTaskService : ITaskService
    {
        public List<ListitTask> UpdatedTasks { get; } = new();
        public List<Guid> DeletedTaskIds { get; } = new();

        public IReadOnlyList<ListitTask> GetAllTasks() => Array.Empty<ListitTask>();
        public ListitTask? GetTask(Guid id) => null;
        public ListitTask CreateTask(string title, TaskType type = TaskType.Recurring, TimeSpan? interval = null, string description = "", int urgency = 1, DateTime? startTime = null, int requiredCompletions = 1, bool bypassPrioritySuppression = false) => throw new NotImplementedException();

        public void UpdateTask(ListitTask task)
        {
            UpdatedTasks.Add(task);
        }

        public bool DeleteTask(Guid id)
        {
            DeletedTaskIds.Add(id);
            return true;
        }
    }

    private readonly OccurrenceService _service = new();

    [Fact]
    public void StartWorking_FromPending_SetsIsWorkingTrue_PreservesPendingStatus()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));

        // Act
        _service.StartWorking(occurrence);

        // Assert
        Assert.True(occurrence.IsWorking);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status);
    }

    [Fact]
    public void StopWorking_FromWorking_SetsIsWorkingFalse_PreservesPendingStatus()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        _service.StartWorking(occurrence);
        Assert.True(occurrence.IsWorking);

        // Act
        _service.StopWorking(occurrence);

        // Assert
        Assert.False(occurrence.IsWorking);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status);
    }

    [Fact]
    public void CompleteOccurrence_SetsCompletedStatus_AndClearsWorking()
    {
        // Arrange
        var task = new ListitTask("Exercise", TaskType.Finite, requiredCompletions: 1);
        var occurrence = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 14, 0, 0));
        _service.StartWorking(occurrence);

        // Act
        _service.CompleteOccurrence(occurrence, task);

        // Assert
        Assert.Equal(OccurrenceStatus.Completed, occurrence.Status);
        Assert.False(occurrence.IsWorking);
        Assert.Equal(1, task.CurrentCompletions);
    }

    [Fact]
    public void CompleteOccurrence_FiniteTask_IncrementsCountPerOccurrence()
    {
        // Arrange
        var task = new ListitTask("Exercise", TaskType.Finite, requiredCompletions: 3);
        var occ1 = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 8, 0, 0));
        var occ2 = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 12, 0, 0));
        var occ3 = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 16, 0, 0));

        // Act & Assert
        _service.CompleteOccurrence(occ1, task);
        Assert.Equal(1, task.CurrentCompletions);

        _service.CompleteOccurrence(occ2, task);
        Assert.Equal(2, task.CurrentCompletions);

        _service.CompleteOccurrence(occ3, task);
        Assert.Equal(3, task.CurrentCompletions);
    }

    [Fact]
    public void CompleteOccurrence_FiniteTask_CannotExceedRequiredCount()
    {
        // Arrange
        var task = new ListitTask("Pay Bills", TaskType.Finite, requiredCompletions: 1, currentCompletions: 1);
        var occ = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 14, 0, 0));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.CompleteOccurrence(occ, task));
        Assert.Equal(1, task.CurrentCompletions);
    }

    [Fact]
    public void CompleteOccurrence_RecurringTask_PreservesIdentity()
    {
        // Arrange
        var task = new ListitTask("Medication", TaskType.Recurring, TimeSpan.FromHours(8), "Daily pills", 4);
        var occurrence = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 14, 0, 0));

        // Act
        _service.CompleteOccurrence(occurrence, task);

        // Assert
        Assert.Equal("Medication", task.Title);
        Assert.Equal(4, task.Urgency);
        Assert.Equal(TimeSpan.FromHours(8), task.Interval);
    }

    [Fact]
    public void CompleteOccurrence_WithTaskService_UpdatesOnPartial_DeletesOnFullCompletion()
    {
        // Arrange
        var fakeTaskService = new FakeTaskService();
        var serviceWithTaskService = new OccurrenceService(fakeTaskService);

        var task = new ListitTask("Two Step Task", TaskType.Finite, requiredCompletions: 2);
        var occ1 = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 10, 0, 0));
        var occ2 = new TaskOccurrence(task.Id, new DateTime(2026, 9, 21, 15, 0, 0));

        // Act 1 - first completion (partial)
        serviceWithTaskService.CompleteOccurrence(occ1, task);

        // Assert 1 - updated, not deleted
        Assert.Single(fakeTaskService.UpdatedTasks);
        Assert.Empty(fakeTaskService.DeletedTaskIds);

        // Act 2 - second completion (reaches required count)
        serviceWithTaskService.CompleteOccurrence(occ2, task);

        // Assert 2 - deleted
        Assert.Single(fakeTaskService.DeletedTaskIds);
        Assert.Equal(task.Id, fakeTaskService.DeletedTaskIds[0]);
    }

    [Fact]
    public void CompleteOccurrence_TaskIdMismatch_ThrowsArgumentException()
    {
        // Arrange
        var task = new ListitTask("Task A", TaskType.Finite, requiredCompletions: 1);
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0)); // Different task ID

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CompleteOccurrence(occurrence, task));
    }

    [Fact]
    public void StartOrStopWorking_OnTerminalOccurrence_ThrowsInvalidOperationException()
    {
        // Arrange
        var completedOcc = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        completedOcc.Complete();

        var missedOcc = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        missedOcc.MarkMissed();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.StartWorking(completedOcc));
        Assert.Throws<InvalidOperationException>(() => _service.StopWorking(completedOcc));
        Assert.Throws<InvalidOperationException>(() => _service.StartWorking(missedOcc));
        Assert.Throws<InvalidOperationException>(() => _service.StopWorking(missedOcc));
    }
}
