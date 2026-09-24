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
    public void Generate_DailyInterval_ProducesOneOccurrencePerDay()
    {
        // Arrange
        var startTime = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask(
            id: Guid.NewGuid(),
            title: "Morning Review",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "",
            urgency: 1,
            startTime: startTime);

        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        var occurrence = Assert.Single(occurrences);
        Assert.Equal(task.Id, occurrence.TaskId);
        Assert.Equal(startTime, occurrence.ScheduledAt);
        Assert.Equal(OccurrenceStatus.Pending, occurrence.Status);
    }

    [Fact]
    public void Generate_SubDayInterval_ProducesMultipleOccurrences()
    {
        // Arrange: starts at 08:00, recurs every 6 hours (08:00, 14:00, 20:00)
        var startTime = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask(
            id: Guid.NewGuid(),
            title: "Hydration",
            type: TaskType.Recurring,
            interval: TimeSpan.FromHours(6),
            description: "",
            urgency: 1,
            startTime: startTime);

        var date = new DateOnly(2026, 9, 21);

        // Act
        var occurrences = _generator.Generate(task, date);

        // Assert
        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc), occurrences[0].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc), occurrences[1].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 21, 20, 0, 0, DateTimeKind.Utc), occurrences[2].ScheduledAt);
    }

    [Fact]
    public void Generate_OrdersOccurrencesChronologicallyByScheduledAtAscending()
    {
        var startTime = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask(
            id: Guid.NewGuid(),
            title: "Checkin",
            type: TaskType.Recurring,
            interval: TimeSpan.FromHours(4),
            description: "",
            urgency: 1,
            startTime: startTime);

        var date = new DateOnly(2026, 9, 21);

        var occurrences = _generator.Generate(task, date);

        Assert.True(occurrences.Count > 1);
        for (int i = 0; i < occurrences.Count - 1; i++)
        {
            Assert.True(occurrences[i].ScheduledAt < occurrences[i + 1].ScheduledAt);
        }
    }

    [Fact]
    public void Generate_EveryOccurrenceReferencesParentTaskId()
    {
        var startTime = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask("Work Hours", TaskType.Recurring, TimeSpan.FromDays(1), startTime: startTime);
        var date = new DateOnly(2026, 9, 21);

        var occurrences = _generator.Generate(task, date);

        Assert.All(occurrences, occ => Assert.Equal(task.Id, occ.TaskId));
    }

    [Fact]
    public void Generate_FiniteTask_ReturnsEmpty_DoesNotManufactureSchedule()
    {
        var finiteTask = new ListitTask("Submit Report", TaskType.Finite, requiredCompletions: 1);
        var date = new DateOnly(2026, 9, 21);

        var occurrences = _generator.Generate(finiteTask, date);

        Assert.Empty(occurrences);
    }

    [Fact]
    public void Generate_MultiDayRange_ProducesAllOccurrencesInChronologicalOrder()
    {
        var startTime = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask("Medicine", TaskType.Recurring, TimeSpan.FromDays(1), startTime: startTime);
        var startDate = new DateOnly(2026, 9, 21);
        var endDate = new DateOnly(2026, 9, 23);

        var occurrences = _generator.Generate(task, startDate, endDate);

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc), occurrences[0].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc), occurrences[1].ScheduledAt);
        Assert.Equal(new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc), occurrences[2].ScheduledAt);
    }

    [Fact]
    public void Generate_ReverseDateRange_ThrowsArgumentException()
    {
        var task = new ListitTask("Sync", TaskType.Recurring);
        var startDate = new DateOnly(2026, 9, 23);
        var endDate = new DateOnly(2026, 9, 21);

        Assert.Throws<ArgumentException>(() => _generator.Generate(task, startDate, endDate));
    }

    [Fact]
    public void Generate_NullTask_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _generator.Generate(null!, new DateOnly(2026, 9, 21)));

        Assert.Throws<ArgumentNullException>(() =>
            _generator.Generate(null!, new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 22)));
    }
}
