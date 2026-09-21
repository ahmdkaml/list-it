using System;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationSuppressionPolicyTests
{
    private readonly NotificationSuppressionPolicy _policy = new();

    [Fact]
    public void Evaluate_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _policy.Evaluate(null!));
    }

    [Fact]
    public void Evaluate_NoActiveWorkingTask_ReturnsAllowedNoActiveWork()
    {
        // Arrange
        var candidateTask = new RecurringTask("Candidate", new[] { new TimeOnly(10, 0) }, urgency: 2);
        var occurrence = new TaskOccurrence(candidateTask.Id, DateTime.Today.AddHours(10));
        var context = new NotificationContext(
            candidateTask,
            occurrence,
            DateTime.Today.AddHours(11),
            SchedulingState.Overdue,
            activeWorkingTask: null);

        // Act
        var result = _policy.Evaluate(context);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedNoActiveWork, result.Reason);
        Assert.Null(result.WorkingTaskId);
        Assert.Null(result.WorkingTaskUrgency);
    }

    [Fact]
    public void Evaluate_CandidateUrgencyHigherThanWorkingTask_ReturnsAllowedHigherPriority()
    {
        // Arrange
        var candidateTask = new RecurringTask("Candidate Urgency 4", new[] { new TimeOnly(10, 0) }, urgency: 4);
        var workingTask = new RecurringTask("Working Urgency 2", new[] { new TimeOnly(9, 0) }, urgency: 2);
        var occurrence = new TaskOccurrence(candidateTask.Id, DateTime.Today.AddHours(10));
        var context = new NotificationContext(
            candidateTask,
            occurrence,
            DateTime.Today.AddHours(11),
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var result = _policy.Evaluate(context);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedHigherOrEqualPriority, result.Reason);
        Assert.Equal(workingTask.Id, result.WorkingTaskId);
        Assert.Equal(2, result.WorkingTaskUrgency);
    }

    [Fact]
    public void Evaluate_CandidateUrgencyEqualToWorkingTask_ReturnsAllowedEqualPriority()
    {
        // Arrange - Equal priority must NOT be suppressed
        var candidateTask = new RecurringTask("Candidate Urgency 3", new[] { new TimeOnly(10, 0) }, urgency: 3);
        var workingTask = new RecurringTask("Working Urgency 3", new[] { new TimeOnly(9, 0) }, urgency: 3);
        var occurrence = new TaskOccurrence(candidateTask.Id, DateTime.Today.AddHours(10));
        var context = new NotificationContext(
            candidateTask,
            occurrence,
            DateTime.Today.AddHours(11),
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var result = _policy.Evaluate(context);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedHigherOrEqualPriority, result.Reason);
        Assert.Equal(workingTask.Id, result.WorkingTaskId);
        Assert.Equal(3, result.WorkingTaskUrgency);
    }

    [Fact]
    public void Evaluate_CandidateUrgencyLowerThanWorkingTask_ReturnsSuppressedByPriority()
    {
        // Arrange
        var candidateTask = new RecurringTask("Candidate Urgency 2", new[] { new TimeOnly(10, 0) }, urgency: 2);
        var workingTask = new RecurringTask("Working Urgency 5", new[] { new TimeOnly(9, 0) }, urgency: 5);
        var occurrence = new TaskOccurrence(candidateTask.Id, DateTime.Today.AddHours(10));
        var context = new NotificationContext(
            candidateTask,
            occurrence,
            DateTime.Today.AddHours(11),
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var result = _policy.Evaluate(context);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonSuppressedByPriority, result.Reason);
        Assert.Equal(workingTask.Id, result.WorkingTaskId);
        Assert.Equal(5, result.WorkingTaskUrgency);
    }

    [Fact]
    public void Evaluate_CandidateWithBypassPrioritySuppression_ReturnsAllowedEvenIfLowerUrgency()
    {
        // Arrange - Candidate has lower urgency (1 vs 6), but BypassPrioritySuppression is true
        var candidateTask = new RecurringTask("Candidate Urgency 1 Bypass", new[] { new TimeOnly(10, 0) }, urgency: 1, bypassPrioritySuppression: true);
        var workingTask = new RecurringTask("Working Urgency 6", new[] { new TimeOnly(9, 0) }, urgency: 6);
        var occurrence = new TaskOccurrence(candidateTask.Id, DateTime.Today.AddHours(10));
        var context = new NotificationContext(
            candidateTask,
            occurrence,
            DateTime.Today.AddHours(11),
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act
        var result = _policy.Evaluate(context);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedBypassPrioritySuppression, result.Reason);
        Assert.Equal(workingTask.Id, result.WorkingTaskId);
        Assert.Equal(6, result.WorkingTaskUrgency);
    }

    [Fact]
    public void Evaluate_WorkingTaskBypassPrioritySuppression_DoesNotAffectCandidateSuppression()
    {
        // CRITICAL OWNERSHIP INVARIANT TEST:
        // The task attempting to notify determines whether it bypasses suppression.
        // The currently working task's BypassPrioritySuppression setting has NO effect.

        // Case A: Working task has bypass=true, candidate has bypass=false -> Candidate is SUPPRESSED
        var candidateA = new RecurringTask("Candidate Urgency 2 Normal", new[] { new TimeOnly(10, 0) }, urgency: 2, bypassPrioritySuppression: false);
        var workingA = new RecurringTask("Working Urgency 5 Bypass", new[] { new TimeOnly(9, 0) }, urgency: 5, bypassPrioritySuppression: true);
        var occA = new TaskOccurrence(candidateA.Id, DateTime.Today.AddHours(10));
        var contextA = new NotificationContext(candidateA, occA, DateTime.Today.AddHours(11), SchedulingState.Overdue, activeWorkingTask: workingA);

        var resultA = _policy.Evaluate(contextA);
        Assert.False(resultA.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonSuppressedByPriority, resultA.Reason);

        // Case B: Working task has bypass=false, candidate has bypass=true -> Candidate is ALLOWED
        var candidateB = new RecurringTask("Candidate Urgency 2 Bypass", new[] { new TimeOnly(10, 0) }, urgency: 2, bypassPrioritySuppression: true);
        var workingB = new RecurringTask("Working Urgency 5 Normal", new[] { new TimeOnly(9, 0) }, urgency: 5, bypassPrioritySuppression: false);
        var occB = new TaskOccurrence(candidateB.Id, DateTime.Today.AddHours(10));
        var contextB = new NotificationContext(candidateB, occB, DateTime.Today.AddHours(11), SchedulingState.Overdue, activeWorkingTask: workingB);

        var resultB = _policy.Evaluate(contextB);
        Assert.True(resultB.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonAllowedBypassPrioritySuppression, resultB.Reason);
    }

    [Fact]
    public void NotificationEngine_WhenOpportunityAllowed_ReturnsNotifyDecisionAndRecordsInHistory()
    {
        // Arrange
        var history = new InMemoryNotificationHistory();
        var timingPolicy = new NotificationTimingPolicy();
        var suppressionPolicy = new NotificationSuppressionPolicy();
        var engine = new NotificationEngine(timingPolicy, suppressionPolicy, history);

        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var candidateTask = new RecurringTask("Candidate Urgency 1", new[] { new TimeOnly(10, 0) }, urgency: 1);
        var occurrence = new TaskOccurrence(candidateTask.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(60); // 100% threshold reached

        var context = new NotificationContext(candidateTask, occurrence, currentTime, SchedulingState.Overdue, activeWorkingTask: null);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.NotNull(decision.SuppressionResult);
        Assert.True(decision.SuppressionResult!.IsAllowed);
        Assert.True(history.HasBeenEmitted(occurrence.OccurrenceId, 0));
    }

    [Fact]
    public void NotificationEngine_WhenOpportunitySuppressed_ReturnsDoNotNotifyAndRecordsInHistory_PreventingRetroactiveFlood()
    {
        // Arrange
        var history = new InMemoryNotificationHistory();
        var timingPolicy = new NotificationTimingPolicy();
        var suppressionPolicy = new NotificationSuppressionPolicy();
        var engine = new NotificationEngine(timingPolicy, suppressionPolicy, history);

        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var candidateTask = new RecurringTask("Candidate Urgency 1", new[] { new TimeOnly(10, 0) }, urgency: 1);
        var workingTask = new RecurringTask("Working Urgency 5", new[] { new TimeOnly(9, 0) }, urgency: 5);
        var occurrence = new TaskOccurrence(candidateTask.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(60); // 100% threshold reached

        var contextWithActiveWork = new NotificationContext(
            candidateTask,
            occurrence,
            currentTime,
            SchedulingState.Overdue,
            activeWorkingTask: workingTask);

        // Act 1: Evaluated while working task is active -> Suppressed
        var decision1 = engine.Evaluate(contextWithActiveWork);

        // Assert 1
        Assert.False(decision1.ShouldNotify);
        Assert.NotNull(decision1.SuppressionResult);
        Assert.False(decision1.SuppressionResult!.IsAllowed);
        Assert.Equal(NotificationSuppressionPolicy.ReasonSuppressedByPriority, decision1.SuppressionResult.Reason);
        // CRITICAL INVARIANT: The opportunity was recorded as emitted even though suppressed
        Assert.True(history.HasBeenEmitted(occurrence.OccurrenceId, 0));

        // Act 2: Active work completes later (activeWorkingTask is now null)
        var contextAfterWorkCompletes = new NotificationContext(
            candidateTask,
            occurrence,
            currentTime.AddMinutes(10),
            SchedulingState.Overdue,
            activeWorkingTask: null);

        var decision2 = engine.Evaluate(contextAfterWorkCompletes);

        // Assert 2: No retroactive flood!
        Assert.False(decision2.ShouldNotify);
    }
}
