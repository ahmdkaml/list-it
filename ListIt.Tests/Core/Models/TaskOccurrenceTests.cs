using System;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class TaskOccurrenceTests
{
    [Fact]
    public void Construction_WithTaskIdAndScheduledAt_InitializesPendingWithNewId()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var scheduledAt = new DateTime(2026, 9, 21, 14, 0, 0);

        // Act
        var occurrence = new TaskOccurrence(taskId, scheduledAt);

        // Assert
        Assert.NotEqual(Guid.Empty, occurrence.OccurrenceId);
        Assert.Equal(taskId, occurrence.TaskId);
        Assert.Equal(scheduledAt, occurrence.ScheduledAt);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status);
    }

    [Fact]
    public void Construction_WithExplicitId_InitializesCorrectly()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var scheduledAt = new DateTime(2026, 9, 21, 8, 30, 0);

        // Act
        var occurrence = new TaskOccurrence(occurrenceId, taskId, scheduledAt, OccurrenceStatus.Pending);

        // Assert
        Assert.Equal(occurrenceId, occurrence.OccurrenceId);
        Assert.Equal(taskId, occurrence.TaskId);
        Assert.Equal(scheduledAt, occurrence.ScheduledAt);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status);
    }

    [Fact]
    public void Construction_EmptyOccurrenceId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TaskOccurrence(Guid.Empty, Guid.NewGuid(), DateTime.Now));
    }

    [Fact]
    public void Construction_EmptyTaskId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TaskOccurrence(Guid.Empty, DateTime.Now));

        Assert.Throws<ArgumentException>(() =>
            new TaskOccurrence(Guid.NewGuid(), Guid.Empty, DateTime.Now));
    }

    [Fact]
    public void Construction_InvalidStatus_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TaskOccurrence(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, (OccurrenceStatus)999));
    }

    [Fact]
    public void ScheduledTime_PreservesExactDateTimeWithoutTimezoneDistortion()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 14, 0, 0, DateTimeKind.Unspecified);
        var taskId = Guid.NewGuid();

        // Act
        var occurrence = new TaskOccurrence(taskId, scheduledAt);

        // Assert
        Assert.Equal(scheduledAt, occurrence.ScheduledAt);
        Assert.Equal(DateTimeKind.Unspecified, occurrence.ScheduledAt.Kind);
        Assert.Equal(14, occurrence.ScheduledAt.Hour);
        Assert.Equal(0, occurrence.ScheduledAt.Minute);
    }

    [Fact]
    public void Complete_FromPending_TransitionsToCompleted()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 9, 0, 0));

        // Act
        occurrence.Complete();

        // Assert
        Assert.Equal(OccurrenceStatus.Completed, occurrence.Status);
    }

    [Fact]
    public void MarkMissed_FromPending_TransitionsToMissed()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 9, 0, 0));

        // Act
        occurrence.MarkMissed();

        // Assert
        Assert.Equal(OccurrenceStatus.Missed, occurrence.Status);
    }

    [Fact]
    public void Complete_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 9, 0, 0));
        occurrence.Complete();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => occurrence.Complete());
        Assert.Contains("Completed", ex.Message);
    }

    [Fact]
    public void MarkMissed_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 9, 0, 0));
        occurrence.Complete();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => occurrence.MarkMissed());
        Assert.Contains("Completed", ex.Message);
    }

    [Fact]
    public void Complete_FromMissed_ThrowsInvalidOperationException()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 9, 0, 0));
        occurrence.MarkMissed();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => occurrence.Complete());
        Assert.Contains("Missed", ex.Message);
    }

    [Fact]
    public void MarkMissed_FromMissed_ThrowsInvalidOperationException()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 9, 0, 0));
        occurrence.MarkMissed();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => occurrence.MarkMissed());
        Assert.Contains("Missed", ex.Message);
    }

    [Fact]
    public void MultipleOccurrences_ForSameTask_HaveIndependentIdentities()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var time1 = new DateTime(2026, 9, 21, 8, 0, 0);
        var time2 = new DateTime(2026, 9, 21, 14, 0, 0);

        // Act
        var occ1 = new TaskOccurrence(taskId, time1);
        var occ2 = new TaskOccurrence(taskId, time2);

        // Assert
        Assert.NotEqual(occ1.OccurrenceId, occ2.OccurrenceId);
        Assert.Equal(occ1.TaskId, occ2.TaskId);
        Assert.Equal(time1, occ1.ScheduledAt);
        Assert.Equal(time2, occ2.ScheduledAt);
    }
}
