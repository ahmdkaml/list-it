using System;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationTimingPolicyTests
{
    private readonly NotificationTimingPolicy _policy = new();
    private readonly InMemoryNotificationHistory _history = new();
    private readonly TimeSpan _lapse = TimeSpan.FromMinutes(60);

    [Fact]
    public void Urgency1_BeforeThreshold_ReturnsNoNotification()
    {
        // Arrange - Urgency 1 threshold is 100% (60m)
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Urgency 1 Task", TaskType.Recurring, urgency: 1);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(59); // 59m < 60m

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act
        var result = _policy.Evaluate(context, _history, _lapse);

        // Assert
        Assert.False(result.ShouldNotify);
        Assert.Empty(result.EligibleOpportunities);
        Assert.Empty(result.AllCrossedOpportunities);
    }

    [Fact]
    public void Urgency1_AtThreshold_ReturnsOpportunity0()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Urgency 1 Task", TaskType.Recurring, urgency: 1);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(60); // Exact 100% threshold

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act
        var result = _policy.Evaluate(context, _history, _lapse);

        // Assert
        Assert.True(result.ShouldNotify);
        var opp = Assert.Single(result.EligibleOpportunities);
        Assert.Equal(0, opp.OpportunityIndex);
        Assert.Equal(1.0, opp.Threshold);
        Assert.Equal(task.Id, opp.TaskId);
        Assert.Equal(occurrence.OccurrenceId, opp.OccurrenceId);
        Assert.Equal(currentTime, opp.ThresholdTime);
    }

    [Fact]
    public void Urgency1_AfterEmitted_RepeatedEvaluationIsIdempotent()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Urgency 1 Task", TaskType.Recurring, urgency: 1);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(75);

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act - First evaluation
        var firstResult = _policy.Evaluate(context, _history, _lapse);
        Assert.True(firstResult.ShouldNotify);

        // Record as emitted
        _history.RecordEmitted(occurrence.OccurrenceId, firstResult.EligibleOpportunities[0].OpportunityIndex);

        // Act - Repeated evaluation
        var secondResult = _policy.Evaluate(context, _history, _lapse);

        // Assert - No new opportunities, but all crossed reflects history
        Assert.False(secondResult.ShouldNotify);
        Assert.Empty(secondResult.EligibleOpportunities);
        Assert.Single(secondResult.AllCrossedOpportunities);
    }

    [Fact]
    public void Urgency2_EvaluatesTwoThresholdsAt50PercentAnd100Percent()
    {
        // Arrange - Urgency 2 has thresholds at 50% (30m) and 100% (60m)
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Urgency 2 Task", TaskType.Recurring, urgency: 2);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);

        // 1. Before 50% (29 minutes)
        var ctx29 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(29), SchedulingState.Overdue);
        var res29 = _policy.Evaluate(ctx29, _history, _lapse);
        Assert.False(res29.ShouldNotify);

        // 2. Exactly at 50% (30 minutes)
        var ctx30 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(30), SchedulingState.Overdue);
        var res30 = _policy.Evaluate(ctx30, _history, _lapse);
        Assert.True(res30.ShouldNotify);
        var opp0 = Assert.Single(res30.EligibleOpportunities);
        Assert.Equal(0, opp0.OpportunityIndex);
        Assert.Equal(0.5, opp0.Threshold);

        // Mark opportunity 0 as emitted
        _history.RecordEmitted(occurrence.OccurrenceId, 0);

        // 3. Between 50% and 100% (45 minutes)
        var ctx45 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(45), SchedulingState.Overdue);
        var res45 = _policy.Evaluate(ctx45, _history, _lapse);
        Assert.False(res45.ShouldNotify);
        Assert.Single(res45.AllCrossedOpportunities);

        // 4. Exactly at 100% (60 minutes)
        var ctx60 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(60), SchedulingState.Overdue);
        var res60 = _policy.Evaluate(ctx60, _history, _lapse);
        Assert.True(res60.ShouldNotify);
        var opp1 = Assert.Single(res60.EligibleOpportunities);
        Assert.Equal(1, opp1.OpportunityIndex);
        Assert.Equal(1.0, opp1.Threshold);

        // Mark opportunity 1 as emitted
        _history.RecordEmitted(occurrence.OccurrenceId, 1);

        // 5. After 100% (75 minutes)
        var ctx75 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(75), SchedulingState.Overdue);
        var res75 = _policy.Evaluate(ctx75, _history, _lapse);
        Assert.False(res75.ShouldNotify);
        Assert.Equal(2, res75.AllCrossedOpportunities.Count);
    }

    [Fact]
    public void MultipleCrossedThresholds_IdentifiesAllCrossedOpportunities()
    {
        // Arrange - Urgency 2 (30m and 60m), evaluated for the first time at 65m
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Urgency 2 Task", TaskType.Recurring, urgency: 2);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(65);

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act
        var result = _policy.Evaluate(context, _history, _lapse);

        // Assert - Both 0 and 1 crossed
        Assert.True(result.ShouldNotify);
        Assert.Equal(2, result.EligibleOpportunities.Count);
        Assert.Equal(2, result.AllCrossedOpportunities.Count);
        Assert.Equal(0, result.EligibleOpportunities[0].OpportunityIndex);
        Assert.Equal(1, result.EligibleOpportunities[1].OpportunityIndex);
    }

    [Fact]
    public void Boundaries_MillisecondPrecisionEvaluation()
    {
        // Arrange - 60 minute lapse = 3600 seconds = 3,600,000 ms. 50% = 1,800,000 ms (30m)
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Urgency 2", TaskType.Recurring, urgency: 2);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);

        // 1 ms before ScheduledAt
        var ctxBeforeScheduled = new NotificationContext(task, occurrence, scheduledAt.AddMilliseconds(-1), SchedulingState.Upcoming);
        Assert.False(_policy.Evaluate(ctxBeforeScheduled, _history, _lapse).ShouldNotify);

        // Exactly at ScheduledAt
        var ctxScheduled = new NotificationContext(task, occurrence, scheduledAt, SchedulingState.Due);
        Assert.False(_policy.Evaluate(ctxScheduled, _history, _lapse).ShouldNotify);

        // 1 ms after ScheduledAt (still well before 50% threshold)
        var ctxJustAfterScheduled = new NotificationContext(task, occurrence, scheduledAt.AddMilliseconds(1), SchedulingState.Overdue);
        Assert.False(_policy.Evaluate(ctxJustAfterScheduled, _history, _lapse).ShouldNotify);

        // 1 ms before 50% threshold (30 minutes - 1ms)
        var ctxBefore50 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(30).AddMilliseconds(-1), SchedulingState.Overdue);
        Assert.False(_policy.Evaluate(ctxBefore50, _history, _lapse).ShouldNotify);

        // Exactly at 50% threshold
        var ctxAt50 = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(30), SchedulingState.Overdue);
        var resAt50 = _policy.Evaluate(ctxAt50, _history, _lapse);
        Assert.True(resAt50.ShouldNotify);
        Assert.Equal(0, resAt50.EligibleOpportunities[0].OpportunityIndex);
    }

    [Fact]
    public void UpcomingOccurrence_ReturnsNoNotification()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 14, 0, 0);
        var task = new ListitTask("Future Task", TaskType.Recurring, urgency: 5);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(-30);

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Upcoming);

        // Act
        var result = _policy.Evaluate(context, _history, _lapse);

        // Assert
        Assert.False(result.ShouldNotify);
        Assert.Empty(result.EligibleOpportunities);
    }

    [Fact]
    public void CompletedOccurrence_EvenWhenOverdue_ReturnsNoNotification()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Completed Task", TaskType.Recurring, urgency: 6);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        occurrence.Complete();

        var currentTime = scheduledAt.AddHours(3); // 3 hours overdue
        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Completed);

        // Act
        var result = _policy.Evaluate(context, _history, _lapse);

        // Assert
        Assert.False(result.ShouldNotify);
        Assert.Empty(result.EligibleOpportunities);
    }

    [Fact]
    public void MissedOccurrence_EvenWhenOverdue_ReturnsNoNotification()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Missed Task", TaskType.Recurring, urgency: 4);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        occurrence.MarkMissed();

        var currentTime = scheduledAt.AddHours(5);
        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Missed);

        // Act
        var result = _policy.Evaluate(context, _history, _lapse);

        // Assert
        Assert.False(result.ShouldNotify);
        Assert.Empty(result.EligibleOpportunities);
    }

    [Fact]
    public void CustomFrequencyPolicy_CanBeInjectedAndEvaluated()
    {
        // Arrange - Custom policy for Urgency 3 with 3 thresholds: 25%, 50%, 75%
        var customPolicies = new System.Collections.Generic.Dictionary<int, NotificationFrequencyPolicy>
        {
            [3] = new(3, new[] { 0.25, 0.50, 0.75 })
        };
        var customPolicyEngine = new NotificationTimingPolicy(customPolicies);

        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new ListitTask("Custom Task", TaskType.Recurring, urgency: 3);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(30); // 50% of 60m

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act
        var result = customPolicyEngine.Evaluate(context, _history, _lapse);

        // Assert - Crossed 0.25 (index 0) and 0.50 (index 1)
        Assert.True(result.ShouldNotify);
        Assert.Equal(2, result.EligibleOpportunities.Count);
        Assert.Equal(0, result.EligibleOpportunities[0].OpportunityIndex);
        Assert.Equal(1, result.EligibleOpportunities[1].OpportunityIndex);
    }
}
