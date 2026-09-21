using System;
using System.Collections.Generic;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Application.Scheduling;

public class SchedulerTests
{
    private readonly Scheduler _scheduler = new();

    [Fact]
    public void Evaluate_Upcoming_WhenCurrentTimeIsBeforeScheduledAt()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        var currentTime = new DateTime(2026, 9, 21, 13, 59, 0);

        // Act
        var state = _scheduler.Evaluate(occurrence, currentTime);

        // Assert
        Assert.Equal(SchedulingState.Upcoming, state);
    }

    [Fact]
    public void Evaluate_Due_WhenCurrentTimeMatchesScheduledAtExactly()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        var currentTime = new DateTime(2026, 9, 21, 14, 0, 0);

        // Act
        var state = _scheduler.Evaluate(occurrence, currentTime);

        // Assert
        Assert.Equal(SchedulingState.Due, state);
    }

    [Fact]
    public void Evaluate_Overdue_WhenCurrentTimeIsAfterScheduledAt()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        var currentTime = new DateTime(2026, 9, 21, 14, 1, 0);

        // Act
        var state = _scheduler.Evaluate(occurrence, currentTime);

        // Assert
        Assert.Equal(SchedulingState.Overdue, state);
    }

    [Fact]
    public void Evaluate_Completed_WhenStatusIsCompleted_EvenBeforeScheduledTime()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        occurrence.Complete();
        var currentTime = new DateTime(2026, 9, 21, 13, 30, 0);

        // Act
        var state = _scheduler.Evaluate(occurrence, currentTime);

        // Assert
        Assert.Equal(SchedulingState.Completed, state);
    }

    [Fact]
    public void Evaluate_Completed_WhenStatusIsCompleted_EvenAfterScheduledTime()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        occurrence.Complete();
        var currentTime = new DateTime(2026, 9, 21, 16, 0, 0);

        // Act
        var state = _scheduler.Evaluate(occurrence, currentTime);

        // Assert
        Assert.Equal(SchedulingState.Completed, state);
    }

    [Fact]
    public void Evaluate_Missed_WhenStatusIsMissed_RegardlessOfCurrentTime()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        occurrence.MarkMissed();

        // Act & Assert - before schedule
        var stateBefore = _scheduler.Evaluate(occurrence, new DateTime(2026, 9, 21, 12, 0, 0));
        Assert.Equal(SchedulingState.Missed, stateBefore);

        // Act & Assert - after schedule
        var stateAfter = _scheduler.Evaluate(occurrence, new DateTime(2026, 9, 21, 16, 0, 0));
        Assert.Equal(SchedulingState.Missed, stateAfter);
    }

    [Fact]
    public void Evaluate_DoesNotMutateOccurrenceState()
    {
        // Arrange
        var occurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 14, 0, 0));
        var currentTime = new DateTime(2026, 9, 21, 15, 0, 0); // After schedule

        // Act
        var state = _scheduler.Evaluate(occurrence, currentTime);

        // Assert
        Assert.Equal(SchedulingState.Overdue, state);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status); // Remains Pending in domain
    }

    [Fact]
    public void Evaluate_BoundaryPrecision_AtOneSecondPrecision()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 14, 0, 0);
        var occurrence = new TaskOccurrence(Guid.NewGuid(), scheduledAt);

        // Act & Assert
        Assert.Equal(SchedulingState.Upcoming, _scheduler.Evaluate(occurrence, new DateTime(2026, 9, 21, 13, 59, 59)));
        Assert.Equal(SchedulingState.Due, _scheduler.Evaluate(occurrence, new DateTime(2026, 9, 21, 14, 0, 0)));
        Assert.Equal(SchedulingState.Overdue, _scheduler.Evaluate(occurrence, new DateTime(2026, 9, 21, 14, 0, 1)));
    }

    [Fact]
    public void Evaluate_DateBoundary_PendingOccurrenceFromYesterdayIsOverdue()
    {
        // Arrange
        var yesterdayOccurrence = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 20, 14, 0, 0));
        var todayTime = new DateTime(2026, 9, 21, 14, 0, 0);

        // Act
        var state = _scheduler.Evaluate(yesterdayOccurrence, todayTime);

        // Assert
        Assert.Equal(SchedulingState.Overdue, state);
    }

    [Fact]
    public void Evaluate_MultipleOccurrences_EvaluatesEachConsistently()
    {
        // Arrange
        var occ1 = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 8, 0, 0));
        var occ2 = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 12, 0, 0));
        var occ3 = new TaskOccurrence(Guid.NewGuid(), new DateTime(2026, 9, 21, 16, 0, 0));
        occ1.Complete();

        var currentTime = new DateTime(2026, 9, 21, 12, 0, 0);

        // Act
        var results = _scheduler.Evaluate(new[] { occ1, occ2, occ3 }, currentTime);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(SchedulingState.Completed, results[occ1]);
        Assert.Equal(SchedulingState.Due, results[occ2]);
        Assert.Equal(SchedulingState.Upcoming, results[occ3]);
    }

    [Fact]
    public void Evaluate_NullArguments_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _scheduler.Evaluate((TaskOccurrence)null!, DateTime.Now));

        Assert.Throws<ArgumentNullException>(() =>
            _scheduler.Evaluate((IEnumerable<TaskOccurrence>)null!, DateTime.Now));
    }
}
