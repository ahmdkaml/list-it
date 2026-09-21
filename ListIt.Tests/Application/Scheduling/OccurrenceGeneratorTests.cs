using System;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Application.Scheduling;

public class OccurrenceGeneratorTests
{
    private readonly OccurrenceGenerator _generator = new();

    [Fact]
    public void Generate_OneAssignedTime_ProducesOneOccurrence()
    {
        // Arrange
        var task = new RecurringTask("Morning Review", new[] { new TimeOnly(8, 0) });
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        var occurrence = Assert.Single(occurrences);
        Assert.Equal(task.Id, occurrence.TaskId);
        Assert.Equal(new DateTime(2026, 9, 21, 8, 0, 0), occurrence.ScheduledAt);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status);
    }

    [Fact]
    public void Generate_MultipleAssignedTimes_ProducesAllOccurrencesWithCorrectTimes()
    {
        // Arrange
        var times = new[] { new TimeOnly(8, 0), new TimeOnly(14, 0), new TimeOnly(20, 0) };
        var task = new RecurringTask("Hydration", times);
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 9, 21, 8, 0, 0), occurrences[0].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 21, 14, 0, 0), occurrences[1].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 21, 20, 0, 0), occurrences[2].ScheduledAt);
    }

    [Fact]
    public void Generate_OrdersOccurrencesChronologicallyByScheduledAtAscending()
    {
        // Arrange
        var times = new[] { new TimeOnly(20, 0), new TimeOnly(8, 0), new TimeOnly(14, 0) };
        var task = new RecurringTask("Checkin", times);
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        Assert.Equal(3, occurrences.Count);
        Assert.True(occurrences[0].ScheduledAt < occurrences[1].ScheduledAt);
        Assert.True(occurrences[1].ScheduledAt < occurrences[2].ScheduledAt);
        Assert.Equal(new TimeOnly(8, 0), TimeOnly.FromDateTime(occurrences[0].ScheduledAt));
        Assert.Equal(new TimeOnly(14, 0), TimeOnly.FromDateTime(occurrences[1].ScheduledAt));
        Assert.Equal(new TimeOnly(20, 0), TimeOnly.FromDateTime(occurrences[2].ScheduledAt));
    }

    [Fact]
    public void Generate_EveryOccurrenceReferencesParentTaskId()
    {
        // Arrange
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(17, 0) };
        var task = new RecurringTask("Work Hours", times);
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        Assert.All(occurrences, occ => Assert.Equal(task.Id, occ.TaskId));
    }

    [Fact]
    public void Generate_EveryOccurrenceStartsWithPendingStatus()
    {
        // Arrange
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(17, 0) };
        var task = new RecurringTask("Work Hours", times);
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        Assert.All(occurrences, occ => Assert.Equal(OccurrenceStatus.Pending, occ.Status));
    }

    [Fact]
    public void Generate_DateAndClockTimeArePreservedWithoutDistortion()
    {
        // Arrange
        var task = new RecurringTask("Meditation", new[] { new TimeOnly(14, 30) });
        var date = new DateOnly(2026, 9, 25);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        var occ = Assert.Single(occurrences);
        Assert.Equal(new DateTime(2026, 9, 25, 14, 30, 0), occ.ScheduledAt);
        Assert.Equal(2026, occ.ScheduledAt.Year);
        Assert.Equal(9, occ.ScheduledAt.Month);
        Assert.Equal(25, occ.ScheduledAt.Day);
        Assert.Equal(14, occ.ScheduledAt.Hour);
        Assert.Equal(30, occ.ScheduledAt.Minute);
    }

    [Fact]
    public void Generate_IsDeterministic_ProducesIdenticalScheduleAndLogicalKeys()
    {
        // Arrange
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(18, 0) };
        var task = new RecurringTask("Daily Standup", times);
        var date = new DateOnly(2026, 9, 21);

        // Act
        var run1 = _generator.Generate(task, date);
        var run2 = _generator.Generate(task, date);

        // Assert
        Assert.Equal(run1.Count, run2.Count);
        for (int i = 0; i < run1.Count; i++)
        {
            Assert.Equal(run1[i].TaskId, run2[i].TaskId);
            Assert.Equal(run1[i].ScheduledAt, run2[i].ScheduledAt);
            Assert.Equal(run1[i].LogicalKey, run2[i].LogicalKey);
            // Each instance has its own object identity
            Assert.NotEqual(Guid.Empty, run1[i].OccurrenceId);
            Assert.NotEqual(Guid.Empty, run2[i].OccurrenceId);
        }
    }

    [Fact]
    public void Generate_DifferentDates_ProducesDifferentScheduledDatesWithSameClockTimes()
    {
        // Arrange
        var task = new RecurringTask("Alarm", new[] { new TimeOnly(7, 0) });
        var date1 = new DateOnly(2026, 9, 21);
        var date2 = new DateOnly(2026, 9, 22);

        // Act
        var occ1 = Assert.Single(_generator.Generate(task, date1));
        var occ2 = Assert.Single(_generator.Generate(task, date2));

        // Assert
        Assert.Equal(new DateTime(2026, 9, 21, 7, 0, 0), occ1.ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 22, 7, 0, 0), occ2.ScheduledAt);
        Assert.NotEqual(occ1.LogicalKey, occ2.LogicalKey);
    }

    [Fact]
    public void Generate_DoesNotMutateSourceTask()
    {
        // Arrange
        var times = new[] { new TimeOnly(10, 0), new TimeOnly(15, 0) };
        var task = new RecurringTask("Focus Blocks", times, "Deep work", 5);
        var date = new DateOnly(2026, 9, 21);

        var originalTitle = task.Title;
        var originalDesc = task.Description;
        var originalUrgency = task.Urgency;
        var originalTimes = task.AssignedTimes.ToList();

        // Act
        _ = _generator.Generate(task, date);

        // Assert
        Assert.Equal(originalTitle, task.Title);
        Assert.Equal(originalDesc, task.Description);
        Assert.Equal(originalUrgency, task.Urgency);
        Assert.Equal(originalTimes, task.AssignedTimes);
    }

    [Fact]
    public void Generate_FiniteTask_ReturnsEmpty_DoesNotManufactureSchedule()
    {
        // Arrange
        var finiteTask = new FiniteTask("Submit Report", requiredCompletions: 1);
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(finiteTask, date);

        // Assert
        Assert.Empty(occurrences);
    }

    [Fact]
    public void Generate_SingleDayRange_ProducesThatDaysOccurrences()
    {
        // Arrange
        var task = new RecurringTask("Sync", new[] { new TimeOnly(11, 0) });
        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date, date);

        // Assert
        var occ = Assert.Single(occurrences);
        Assert.Equal(new DateTime(2026, 9, 21, 11, 0, 0), occ.ScheduledAt);
    }

    [Fact]
    public void Generate_MultiDayRange_ProducesAllOccurrencesInChronologicalOrder()
    {
        // Arrange
        var times = new[] { new TimeOnly(8, 0), new TimeOnly(20, 0) };
        var task = new RecurringTask("Medicine", times);
        var startDate = new DateOnly(2026, 9, 21);
        var endDate = new DateOnly(2026, 9, 23);

        // Act
        var occurrences = _generator.Generate(task, startDate, endDate);

        // Assert: 3 days * 2 times = 6 occurrences
        Assert.Equal(6, occurrences.Count);
        Assert.Equal(new DateTime(2026, 9, 21, 8, 0, 0), occurrences[0].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 21, 20, 0, 0), occurrences[1].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 22, 8, 0, 0), occurrences[2].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 22, 20, 0, 0), occurrences[3].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 23, 8, 0, 0), occurrences[4].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 23, 20, 0, 0), occurrences[5].ScheduledAt);
    }

    [Fact]
    public void Generate_ReverseDateRange_ThrowsArgumentException()
    {
        // Arrange
        var task = new RecurringTask("Sync", new[] { new TimeOnly(11, 0) });
        var startDate = new DateOnly(2026, 9, 23);
        var endDate = new DateOnly(2026, 9, 21);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _generator.Generate(task, startDate, endDate));
    }

    [Fact]
    public void Generate_NullTask_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _generator.Generate(null!, new DateOnly(2026, 9, 21)));

        Assert.Throws<ArgumentNullException>(() =>
            _generator.Generate(null!, new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 22)));
    }
}
