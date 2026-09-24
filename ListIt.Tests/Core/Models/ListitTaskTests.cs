using System;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class ListitTaskTests
{
    [Fact]
    public void ValidTask_CanBeCreated_WithDefaults()
    {
        var task = new ListitTask("Buy groceries");

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Buy groceries", task.Title);
        Assert.Equal(string.Empty, task.Description);
        Assert.Equal(1, task.Urgency);
        Assert.Equal(TaskType.Recurring, task.Type);
        Assert.Equal(TimeSpan.FromDays(1), task.Interval);
        Assert.Equal(0, task.Passes);
        Assert.Equal(0, task.CurrentCompletions);
        Assert.Equal(1, task.RequiredCompletions);
        Assert.False(task.BypassPrioritySuppression);
        Assert.Equal(DateTimeKind.Utc, task.StartTime.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceTitle_ThrowsArgumentException(string? invalidTitle)
    {
        var ex = Assert.Throws<ArgumentException>(() => new ListitTask(invalidTitle!));
        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void EmptyId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ListitTask(
            id: Guid.Empty,
            title: "Test",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "",
            urgency: 1,
            startTime: DateTime.UtcNow));

        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void NonUtcStartTime_ThrowsArgumentException()
    {
        var localTime = DateTime.Now;
        var ex = Assert.Throws<ArgumentException>(() => new ListitTask(
            id: Guid.NewGuid(),
            title: "Test",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "",
            urgency: 1,
            startTime: localTime));

        Assert.Equal("startTime", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroOrNegativeInterval_ThrowsArgumentOutOfRangeException(int minutes)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new ListitTask(
            id: Guid.NewGuid(),
            title: "Test",
            type: TaskType.Recurring,
            interval: TimeSpan.FromMinutes(minutes),
            description: "",
            urgency: 1,
            startTime: DateTime.UtcNow));

        Assert.Equal("interval", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void InvalidUrgency_ThrowsArgumentOutOfRangeException(int urgency)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new ListitTask("Test", urgency: urgency));
        Assert.Equal("urgency", ex.ParamName);
    }

    [Fact]
    public void NegativePasses_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new ListitTask(
            id: Guid.NewGuid(),
            title: "Test",
            type: TaskType.Recurring,
            interval: TimeSpan.FromDays(1),
            description: "",
            urgency: 1,
            startTime: DateTime.UtcNow,
            passes: -1));

        Assert.Equal("passes", ex.ParamName);
    }

    [Fact]
    public void FiniteTask_InvalidCompletions_Throws()
    {
        // required < 1
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListitTask(
            title: "Test",
            type: TaskType.Finite,
            requiredCompletions: 0));

        // current < 0
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListitTask(
            title: "Test",
            type: TaskType.Finite,
            requiredCompletions: 2,
            currentCompletions: -1));

        // current > required
        Assert.Throws<ArgumentException>(() => new ListitTask(
            title: "Test",
            type: TaskType.Finite,
            requiredCompletions: 2,
            currentCompletions: 3));
    }

    [Fact]
    public void FiniteTask_RecordCompletion_IncrementsUntilLimit()
    {
        var task = new ListitTask("Test", type: TaskType.Finite, requiredCompletions: 2);
        Assert.Equal(0, task.CurrentCompletions);

        task.RecordCompletion();
        Assert.Equal(1, task.CurrentCompletions);

        task.RecordCompletion();
        Assert.Equal(2, task.CurrentCompletions);

        Assert.Throws<InvalidOperationException>(() => task.RecordCompletion());
    }

    [Fact]
    public void RecurringTask_RecordCompletion_IncrementsWithoutLimit()
    {
        var task = new ListitTask("Test", type: TaskType.Recurring);
        task.RecordCompletion();
        task.RecordCompletion();
        Assert.Equal(2, task.CurrentCompletions);
    }

    [Fact]
    public void RecordPass_IncrementsPasses_AndCalculatesNextDeadline()
    {
        var startTime = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(4);
        var task = new ListitTask(
            id: Guid.NewGuid(),
            title: "Test",
            type: TaskType.Recurring,
            interval: interval,
            description: "",
            urgency: 1,
            startTime: startTime);

        Assert.Equal(0, task.Passes);
        Assert.Equal(startTime.AddHours(4), task.GetNextDeadlineUtc());

        task.RecordPass();
        Assert.Equal(1, task.Passes);
        Assert.Equal(startTime.AddHours(8), task.GetNextDeadlineUtc());

        task.RecordPass();
        Assert.Equal(2, task.Passes);
        Assert.Equal(startTime.AddHours(12), task.GetNextDeadlineUtc());

        task.ResetPasses();
        Assert.Equal(0, task.Passes);
        Assert.Equal(startTime.AddHours(4), task.GetNextDeadlineUtc());
    }

    [Fact]
    public void UpdateDetails_SetsTitleAndDescription()
    {
        var task = new ListitTask("Old Title", description: "Old Desc");
        task.UpdateDetails("New Title", "New Desc");

        Assert.Equal("New Title", task.Title);
        Assert.Equal("New Desc", task.Description);
    }
}
