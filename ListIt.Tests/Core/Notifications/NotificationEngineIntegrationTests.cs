using System;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationEngineIntegrationTests
{
    private readonly InMemoryNotificationHistory _history = new();
    private readonly NotificationTimingPolicy _timingPolicy = new();
    private readonly NotificationSuppressionPolicy _suppressionPolicy = new();
    private readonly NotificationPresentationPolicy _presentationPolicy = new();
    private readonly NotificationEngine _engine;

    public NotificationEngineIntegrationTests()
    {
        _engine = new NotificationEngine(
            _timingPolicy,
            _suppressionPolicy,
            _history,
            _presentationPolicy);
    }

    [Fact]
    public void Scenario1_Allowed_OpportunityExists_NoActiveWork_Notifies()
    {
        // Arrange - Urgency 3 task, scheduled at 10:00, evaluated at 10:30 (50% threshold crossed)
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new RecurringTask("Task 1", new[] { new TimeOnly(10, 0) }, urgency: 3);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(30);

        var context = new NotificationContext(
            task,
            occurrence,
            currentTime,
            SchedulingState.Overdue,
            skipCount: 2,
            activeWorkingTask: null);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(task.Id, decision.TaskId);
        Assert.Equal(occurrence.OccurrenceId, decision.OccurrenceId);
        Assert.Equal(3, decision.Urgency);
        Assert.Equal(2, decision.SkipCount);
        Assert.Equal(NotificationVisualCategory.Medium, decision.VisualCategory);
        Assert.Equal(0.625, decision.Opacity, precision: 3);
        Assert.NotNull(decision.SuppressionResult);
        Assert.True(decision.SuppressionResult!.IsAllowed);
        Assert.NotEmpty(decision.Opportunities);
    }

    [Fact]
    public void Scenario2_LowerPriority_OpportunityExists_WorkingUrgency5_CandidateUrgency3_Suppressed()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var candidateTask = new RecurringTask("Candidate Urgency 3", new[] { new TimeOnly(10, 0) }, urgency: 3, bypassPrioritySuppression: false);
        var workingTask = new RecurringTask("Working Urgency 5", new[] { new TimeOnly(9, 0) }, urgency: 5);
        var occurrence = new TaskOccurrence(candidateTask.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(30);

        var context = new NotificationContext(
            candidateTask,
            occurrence,
            currentTime,
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldNotify);
        Assert.Equal(0.0, decision.Opacity);
        Assert.NotNull(decision.SuppressionResult);
        Assert.False(decision.SuppressionResult!.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonSuppressedByPriority, decision.SuppressionResult.Reason);
    }

    [Fact]
    public void Scenario3_CandidateBypass_OpportunityExists_WorkingUrgency5_CandidateUrgency2_BypassTrue_Allowed()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var candidateTask = new RecurringTask("Candidate Urgency 2 Bypass", new[] { new TimeOnly(10, 0) }, urgency: 2, bypassPrioritySuppression: true);
        var workingTask = new RecurringTask("Working Urgency 5", new[] { new TimeOnly(9, 0) }, urgency: 5);
        var occurrence = new TaskOccurrence(candidateTask.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(30);

        var context = new NotificationContext(
            candidateTask,
            occurrence,
            currentTime,
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(NotificationVisualCategory.Low, decision.VisualCategory);
        Assert.NotNull(decision.SuppressionResult);
        Assert.True(decision.SuppressionResult!.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedBypassPrioritySuppression, decision.SuppressionResult.Reason);
    }

    [Fact]
    public void Scenario4_HigherPriorityCandidate_WorkingUrgency3_CandidateUrgency5_BypassFalse_Allowed()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var candidateTask = new RecurringTask("Candidate Urgency 5", new[] { new TimeOnly(10, 0) }, urgency: 5, bypassPrioritySuppression: false);
        var workingTask = new RecurringTask("Working Urgency 3", new[] { new TimeOnly(9, 0) }, urgency: 3);
        var occurrence = new TaskOccurrence(candidateTask.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(30);

        var context = new NotificationContext(
            candidateTask,
            occurrence,
            currentTime,
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(NotificationVisualCategory.High, decision.VisualCategory);
        Assert.NotNull(decision.SuppressionResult);
        Assert.True(decision.SuppressionResult!.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedHigherOrEqualPriority, decision.SuppressionResult.Reason);
    }

    [Fact]
    public void Scenario5_EqualPriority_WorkingUrgency4_CandidateUrgency4_BypassFalse_Allowed()
    {
        // Arrange - Equal priority must not be suppressed
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var candidateTask = new RecurringTask("Candidate Urgency 4", new[] { new TimeOnly(10, 0) }, urgency: 4, bypassPrioritySuppression: false);
        var workingTask = new RecurringTask("Working Urgency 4", new[] { new TimeOnly(9, 0) }, urgency: 4);
        var occurrence = new TaskOccurrence(candidateTask.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(30);

        var context = new NotificationContext(
            candidateTask,
            occurrence,
            currentTime,
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(NotificationVisualCategory.Medium, decision.VisualCategory);
        Assert.NotNull(decision.SuppressionResult);
        Assert.True(decision.SuppressionResult!.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedHigherOrEqualPriority, decision.SuppressionResult.Reason);
    }

    [Fact]
    public void Scenario6_CompletedOccurrence_ProducesNoNotification()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new RecurringTask("Completed Task", new[] { new TimeOnly(10, 0) }, urgency: 5);
        var occurrence = new TaskOccurrence(Guid.NewGuid(), task.Id, scheduledAt, OccurrenceStatus.Completed);
        var currentTime = scheduledAt.AddMinutes(45);

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldNotify);
        Assert.Equal(0.0, decision.Opacity);
        Assert.Equal(OccurrenceStatus.Completed, occurrence.Status);
    }

    [Fact]
    public void Scenario7_MissedOccurrence_ProducesNoNotification()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new RecurringTask("Missed Task", new[] { new TimeOnly(10, 0) }, urgency: 5);
        var occurrence = new TaskOccurrence(Guid.NewGuid(), task.Id, scheduledAt, OccurrenceStatus.Missed);
        var currentTime = scheduledAt.AddMinutes(45);

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act
        var decision = _engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldNotify);
        Assert.Equal(0.0, decision.Opacity);
        Assert.Equal(OccurrenceStatus.Missed, occurrence.Status);
    }

    [Fact]
    public void Scenario8_RepeatedEvaluation_IsIdempotentAndDoesNotSpam()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new RecurringTask("Idempotency Task", new[] { new TimeOnly(10, 0) }, urgency: 1);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(60); // 100% threshold crossed

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);

        // Act 1 - First evaluation
        var decision1 = _engine.Evaluate(context);

        // Assert 1 - Notifies once
        Assert.True(decision1.ShouldNotify);
        Assert.True(_history.HasBeenEmitted(occurrence.OccurrenceId, 0));

        // Act 2 - Immediate re-evaluation at same time
        var decision2 = _engine.Evaluate(context);

        // Assert 2 - Idempotent, no duplicate notification
        Assert.False(decision2.ShouldNotify);

        // Act 3 - Subsequent evaluation 5 minutes later before any new threshold
        var contextLater = new NotificationContext(task, occurrence, currentTime.AddMinutes(5), SchedulingState.Overdue);
        var decision3 = _engine.Evaluate(contextLater);

        // Assert 3 - Still no duplicate notification
        Assert.False(decision3.ShouldNotify);
    }

    [Fact]
    public void Scenario9_MultipleCrossedOpportunities_SimulatesLargeTimeJump_ProcessesAllOpportunities()
    {
        // Arrange - Urgency 4 has thresholds at 25% (15m), 50% (30m), 75% (45m), 100% (60m)
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new RecurringTask("Urgency 4 Task", new[] { new TimeOnly(10, 0) }, urgency: 4);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);

        // Time jumps directly to 10:45 (45 minutes elapsed = 75% of 60m lapse)
        // This crosses 3 thresholds: 0 (25%), 1 (50%), 2 (75%)
        var jumpTime = scheduledAt.AddMinutes(45);
        var contextJump = new NotificationContext(task, occurrence, jumpTime, SchedulingState.Overdue);

        // Act 1
        var decisionJump = _engine.Evaluate(contextJump);

        // Assert 1
        Assert.True(decisionJump.ShouldNotify);
        Assert.Equal(3, decisionJump.Opportunities.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.True(_history.HasBeenEmitted(occurrence.OccurrenceId, i));
        }

        // Act 2 - Next tick at 10:46 (no new threshold crossed)
        var contextNextTick = new NotificationContext(task, occurrence, jumpTime.AddMinutes(1), SchedulingState.Overdue);
        var decisionNextTick = _engine.Evaluate(contextNextTick);

        // Assert 2 - No new notification
        Assert.False(decisionNextTick.ShouldNotify);

        // Act 3 - Time reaches 11:00 (60 minutes elapsed = 100% threshold crossed)
        var context100Percent = new NotificationContext(task, occurrence, scheduledAt.AddMinutes(60), SchedulingState.Overdue);
        var decision100Percent = _engine.Evaluate(context100Percent);

        // Assert 3 - Exactly the 4th opportunity (index 3) notifies
        Assert.True(decision100Percent.ShouldNotify);
        var singleOpp = Assert.Single(decision100Percent.Opportunities);
        Assert.Equal(3, singleOpp.OpportunityIndex);
        Assert.Equal(1.0, singleOpp.Threshold);
    }
}
