using System;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationContextTests
{
    [Fact]
    public void Constructor_ValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var task = new RecurringTask("Morning Sync", new[] { new TimeOnly(9, 0) }, "Standup meeting", 3, bypassPrioritySuppression: true);
        var scheduledAt = new DateTime(2026, 9, 21, 9, 0, 0);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = new DateTime(2026, 9, 21, 9, 0, 0);
        var workingTask = new FiniteTask("Critical Bug", requiredCompletions: 1, urgency: 5);

        // Act
        var context = new NotificationContext(
            task,
            occurrence,
            currentTime,
            SchedulingState.Due,
            skipCount: 2,
            activeWorkingTask: workingTask);

        // Assert
        Assert.Same(task, context.Task);
        Assert.Same(occurrence, context.Occurrence);
        Assert.Equal(currentTime, context.CurrentTime);
        Assert.Equal(SchedulingState.Due, context.SchedulingState);
        Assert.Equal(2, context.SkipCount);
        Assert.Same(workingTask, context.ActiveWorkingTask);

        // Convenience accessors
        Assert.Equal(task.Id, context.TaskId);
        Assert.Equal(occurrence.OccurrenceId, context.OccurrenceId);
        Assert.Equal(scheduledAt, context.ScheduledAt);
        Assert.Equal(3, context.Urgency);
        Assert.True(context.BypassPrioritySuppression);
        Assert.False(context.IsOccurrenceWorking);
        Assert.True(context.HasActiveWorkingTask);
        Assert.Equal(5, context.ActiveWorkingTaskUrgency);
    }

    [Fact]
    public void Constructor_NullTask_ThrowsArgumentNullException()
    {
        var occurrence = new TaskOccurrence(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<ArgumentNullException>(() =>
            new NotificationContext(null!, occurrence, DateTime.UtcNow, SchedulingState.Upcoming));
    }

    [Fact]
    public void Constructor_NullOccurrence_ThrowsArgumentNullException()
    {
        var task = new RecurringTask("Task", new[] { new TimeOnly(9, 0) });

        Assert.Throws<ArgumentNullException>(() =>
            new NotificationContext(task, null!, DateTime.UtcNow, SchedulingState.Upcoming));
    }

    [Fact]
    public void Constructor_TaskIdMismatch_ThrowsArgumentException()
    {
        var task = new RecurringTask("Task 1", new[] { new TimeOnly(9, 0) });
        var otherTaskId = Guid.NewGuid();
        var occurrence = new TaskOccurrence(otherTaskId, DateTime.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            new NotificationContext(task, occurrence, DateTime.UtcNow, SchedulingState.Upcoming));
        Assert.Contains("Occurrence TaskId does not match", ex.Message);
    }

    [Fact]
    public void Constructor_NegativeSkipCount_ThrowsArgumentOutOfRangeException()
    {
        var task = new RecurringTask("Task", new[] { new TimeOnly(9, 0) });
        var occurrence = new TaskOccurrence(task.Id, DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NotificationContext(task, occurrence, DateTime.UtcNow, SchedulingState.Upcoming, skipCount: -1));
    }

    [Fact]
    public void BypassPrioritySuppression_ReflectsEvaluatedTaskSetting()
    {
        // Arrange
        var bypassedTask = new RecurringTask("Bypassed", new[] { new TimeOnly(10, 0) }, bypassPrioritySuppression: true);
        var normalTask = new RecurringTask("Normal", new[] { new TimeOnly(10, 0) }, bypassPrioritySuppression: false);

        var occBypassed = new TaskOccurrence(bypassedTask.Id, DateTime.UtcNow);
        var occNormal = new TaskOccurrence(normalTask.Id, DateTime.UtcNow);

        // Act
        var contextBypassed = new NotificationContext(bypassedTask, occBypassed, DateTime.UtcNow, SchedulingState.Due);
        var contextNormal = new NotificationContext(normalTask, occNormal, DateTime.UtcNow, SchedulingState.Due);

        // Assert
        Assert.True(contextBypassed.BypassPrioritySuppression);
        Assert.False(contextNormal.BypassPrioritySuppression);
    }

    [Fact]
    public void WorkingState_ReflectsOccurrenceWorkingState()
    {
        // Arrange
        var task = new RecurringTask("Task", new[] { new TimeOnly(9, 0) });
        var occurrence = new TaskOccurrence(task.Id, DateTime.UtcNow);

        var contextBefore = new NotificationContext(task, occurrence, DateTime.UtcNow, SchedulingState.Due);
        Assert.False(contextBefore.IsOccurrenceWorking);

        // Act
        occurrence.StartWorking();
        var contextAfter = new NotificationContext(task, occurrence, DateTime.UtcNow, SchedulingState.Due);

        // Assert
        Assert.True(contextAfter.IsOccurrenceWorking);
    }

    [Fact]
    public void ActiveWorkingTask_WhenNull_HasActiveWorkingTaskIsFalse()
    {
        // Arrange
        var task = new RecurringTask("Task", new[] { new TimeOnly(9, 0) });
        var occurrence = new TaskOccurrence(task.Id, DateTime.UtcNow);

        // Act
        var context = new NotificationContext(task, occurrence, DateTime.UtcNow, SchedulingState.Upcoming, activeWorkingTask: null);

        // Assert
        Assert.False(context.HasActiveWorkingTask);
        Assert.Null(context.ActiveWorkingTask);
        Assert.Null(context.ActiveWorkingTaskUrgency);
    }

    [Fact]
    public void Determinism_IdenticalInputs_ProducesIdenticalProperties()
    {
        // Arrange
        var task = new RecurringTask("Daily Review", new[] { new TimeOnly(8, 30) }, urgency: 4);
        var time = new DateTime(2026, 9, 21, 8, 30, 0);
        var occurrence = new TaskOccurrence(task.Id, time);

        // Act
        var context1 = new NotificationContext(task, occurrence, time, SchedulingState.Due, skipCount: 1);
        var context2 = new NotificationContext(task, occurrence, time, SchedulingState.Due, skipCount: 1);

        // Assert
        Assert.Equal(context1.TaskId, context2.TaskId);
        Assert.Equal(context1.OccurrenceId, context2.OccurrenceId);
        Assert.Equal(context1.CurrentTime, context2.CurrentTime);
        Assert.Equal(context1.SchedulingState, context2.SchedulingState);
        Assert.Equal(context1.SkipCount, context2.SkipCount);
        Assert.Equal(context1.Urgency, context2.Urgency);
        Assert.Equal(context1.BypassPrioritySuppression, context2.BypassPrioritySuppression);
    }
}
