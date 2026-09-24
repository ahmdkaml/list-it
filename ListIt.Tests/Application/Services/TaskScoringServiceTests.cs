using System;
using ListIt.Core.Models;
using ListIt.Core.Services;
using Xunit;

namespace ListIt.Tests.Application.Services;

public class TaskScoringServiceTests
{
    private readonly TaskScoringService _service = new();

    [Fact]
    public void CalculateScore_NullTask_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.CalculateScore(null!, DateTime.UtcNow));
    }

    [Fact]
    public void CalculateScore_BaseUrgency_ScalesLinearlyAtStartOfWindow()
    {
        var startTime = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2);
        var taskU1 = new ListitTask("U1", startTime: startTime, interval: interval, urgency: 1);
        var taskU3 = new ListitTask("U3", startTime: startTime, interval: interval, urgency: 3);
        var taskU6 = new ListitTask("U6", startTime: startTime, interval: interval, urgency: 6);

        // At window start (10:00:00), timeScore is 0, passes = 0, work = 0
        var scoreU1 = _service.CalculateScore(taskU1, startTime);
        var scoreU3 = _service.CalculateScore(taskU3, startTime);
        var scoreU6 = _service.CalculateScore(taskU6, startTime);

        Assert.Equal(100.0, scoreU1);
        Assert.Equal(300.0, scoreU3);
        Assert.Equal(600.0, scoreU6);
        Assert.True(scoreU6 > scoreU3);
        Assert.True(scoreU3 > scoreU1);
    }

    [Fact]
    public void CalculateScore_DeadlineProximity_IncreasesScoreAsDeadlineApproaches()
    {
        var startTime = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2); // Deadline is 12:00
        var task = new ListitTask("Task", startTime: startTime, interval: interval, urgency: 1);

        // 1. At start (10:00): progress = 0.0 -> score = 100.0
        var score0 = _service.CalculateScore(task, startTime);
        Assert.Equal(100.0, score0);

        // 2. At half-time (11:00): progress = 0.5 -> timeScore = 50 * 0.25 = 12.5 -> score = 112.5
        var score50 = _service.CalculateScore(task, startTime.AddHours(1));
        Assert.Equal(112.5, score50);

        // 3. At deadline (12:00): progress = 1.0 -> timeScore = 50 * 1.0 = 50.0 -> score = 150.0
        var score100 = _service.CalculateScore(task, startTime.AddHours(2));
        Assert.Equal(150.0, score100);

        // 4. Overdue by 1 hour (13:00): progress = 1.5 -> timeScore = 50 + 50 * 0.5 = 75.0 -> score = 175.0
        var scoreOverdue = _service.CalculateScore(task, startTime.AddHours(3));
        Assert.Equal(175.0, scoreOverdue);

        Assert.True(scoreOverdue > score100);
        Assert.True(score100 > score50);
        Assert.True(score50 > score0);
    }

    [Fact]
    public void CalculateScore_PassCount_AddsPassPenaltyMultiplier()
    {
        var startTime = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var task0Passes = new ListitTask("0 Passes", startTime: startTime, interval: interval, urgency: 2, passes: 0);
        var task2Passes = new ListitTask("2 Passes", startTime: startTime, interval: interval, urgency: 2, passes: 2);

        var score0 = _service.CalculateScore(task0Passes, startTime);
        var score2 = _service.CalculateScore(task2Passes, startTime);

        // Each pass adds +75.0 points (2 passes = +150.0)
        Assert.Equal(200.0, score0);
        Assert.Equal(350.0, score2);
        Assert.Equal(150.0, score2 - score0);
    }

    [Fact]
    public void CalculateScore_WorkingState_ProvidesProminentBoost()
    {
        var startTime = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);
        var taskLowUrgency = new ListitTask("Low Urgency", startTime: startTime, urgency: 1);
        var taskHighUrgency = new ListitTask("High Urgency", startTime: startTime, urgency: 6);

        var nonWorkingScore = _service.CalculateScore(taskHighUrgency, startTime, isWorking: false);
        var workingScore = _service.CalculateScore(taskLowUrgency, startTime, isWorking: true);

        // High urgency score = 600.0, Working low urgency = 1000 + 100 = 1100.0
        Assert.Equal(600.0, nonWorkingScore);
        Assert.Equal(1100.0, workingScore);
        Assert.True(workingScore > nonWorkingScore);
    }
}
