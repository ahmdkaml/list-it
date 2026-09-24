using System;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class HalvingNotificationTimingPolicyTests
{
    private readonly HalvingNotificationTimingPolicy _policy = new();
    private readonly InMemoryNotificationHistory _history = new();

    [Fact]
    public void CalculateHalvingThresholds_ProducesExactMathematicalFractions()
    {
        var thresholds = HalvingNotificationTimingPolicy.CalculateHalvingThresholds(5);

        Assert.Equal(5, thresholds.Count);
        Assert.Equal(0.50, thresholds[0], precision: 6);       // 50%
        Assert.Equal(0.75, thresholds[1], precision: 6);       // 75%
        Assert.Equal(0.875, thresholds[2], precision: 6);      // 87.5%
        Assert.Equal(0.9375, thresholds[3], precision: 6);     // 93.75%
        Assert.Equal(0.96875, thresholds[4], precision: 6);    // 96.875%
    }

    [Fact]
    public void FirstAlert_FiresAt50PercentOfIntervalDuration()
    {
        // 2-hour interval: window from 10:00 to 12:00
        var deadline = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2);
        var startTime = deadline - interval; // 10:00

        var task = new ListitTask("2h Task", startTime: startTime, interval: interval);
        var occurrence = new TaskOccurrence(task.Id, deadline);

        // Before 50%: 10:59 (59 minutes elapsed = 49.16% of 120m)
        var ctxBefore = new NotificationContext(task, occurrence, startTime.AddMinutes(59), SchedulingState.Upcoming);
        var resBefore = _policy.Evaluate(ctxBefore, _history);
        Assert.False(resBefore.ShouldNotify);
        Assert.Empty(resBefore.EligibleOpportunities);

        // At 50%: 11:00 (60 minutes elapsed = 50% of 120m)
        var ctxAt50 = new NotificationContext(task, occurrence, startTime.AddMinutes(60), SchedulingState.Upcoming);
        var resAt50 = _policy.Evaluate(ctxAt50, _history);
        Assert.True(resAt50.ShouldNotify);
        var opp = Assert.Single(resAt50.EligibleOpportunities);
        Assert.Equal(0, opp.OpportunityIndex);
        Assert.Equal(0.50, opp.Threshold, precision: 6);
        Assert.Equal(startTime.AddMinutes(60), opp.ThresholdTime);
    }

    [Fact]
    public void SecondAlert_FiresAt75PercentOfIntervalDuration()
    {
        var deadline = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2);
        var startTime = deadline - interval; // 10:00

        var task = new ListitTask("2h Task", startTime: startTime, interval: interval);
        var occurrence = new TaskOccurrence(task.Id, deadline);

        // Simulate Opportunity 0 already emitted
        _history.RecordEmitted(occurrence.OccurrenceId, 0);

        // At 74 minutes (61.6%): Opportunity 1 not reached
        var ctx74 = new NotificationContext(task, occurrence, startTime.AddMinutes(74), SchedulingState.Upcoming);
        var res74 = _policy.Evaluate(ctx74, _history);
        Assert.False(res74.ShouldNotify);

        // At 90 minutes (75% of 120m = 90m): Opportunity 1 crossed
        var ctx90 = new NotificationContext(task, occurrence, startTime.AddMinutes(90), SchedulingState.Upcoming);
        var res90 = _policy.Evaluate(ctx90, _history);
        Assert.True(res90.ShouldNotify);
        var opp = Assert.Single(res90.EligibleOpportunities);
        Assert.Equal(1, opp.OpportunityIndex);
        Assert.Equal(0.75, opp.Threshold, precision: 6);
        Assert.Equal(startTime.AddMinutes(90), opp.ThresholdTime);
    }

    [Fact]
    public void ThirdAlert_FiresAt87Point5PercentOfIntervalDuration()
    {
        var deadline = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2);
        var startTime = deadline - interval; // 10:00

        var task = new ListitTask("2h Task", startTime: startTime, interval: interval);
        var occurrence = new TaskOccurrence(task.Id, deadline);

        // Opportunities 0 and 1 already emitted
        _history.RecordEmitted(occurrence.OccurrenceId, 0);
        _history.RecordEmitted(occurrence.OccurrenceId, 1);

        // At 105 minutes (87.5% of 120m = 105m): Opportunity 2 crossed
        var ctx105 = new NotificationContext(task, occurrence, startTime.AddMinutes(105), SchedulingState.Upcoming);
        var res105 = _policy.Evaluate(ctx105, _history);
        Assert.True(res105.ShouldNotify);
        var opp = Assert.Single(res105.EligibleOpportunities);
        Assert.Equal(2, opp.OpportunityIndex);
        Assert.Equal(0.875, opp.Threshold, precision: 6);
        Assert.Equal(startTime.AddMinutes(105), opp.ThresholdTime);
    }

    [Fact]
    public void TerminalOccurrences_DoNotNotify()
    {
        var deadline = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask("Terminal Task", interval: TimeSpan.FromHours(1));
        var completedOcc = new TaskOccurrence(Guid.NewGuid(), task.Id, deadline, OccurrenceStatus.Completed);
        var missedOcc = new TaskOccurrence(Guid.NewGuid(), task.Id, deadline, OccurrenceStatus.Missed);

        var ctxCompleted = new NotificationContext(task, completedOcc, deadline, SchedulingState.Completed);
        var ctxMissed = new NotificationContext(task, missedOcc, deadline, SchedulingState.Missed);

        Assert.False(_policy.Evaluate(ctxCompleted, _history).ShouldNotify);
        Assert.False(_policy.Evaluate(ctxMissed, _history).ShouldNotify);
    }

    [Fact]
    public void OpacityProgression_IncreasesWithOpportunityIndex()
    {
        var presentation = new NotificationPresentationPolicy();

        // Alert 0: Baseline opacity (0.50)
        Assert.Equal(0.50, presentation.CalculateOpacity(skipCount: 0, opportunityIndex: 0), precision: 3);

        // Alert 1: 0.625
        Assert.Equal(0.625, presentation.CalculateOpacity(skipCount: 0, opportunityIndex: 1), precision: 3);

        // Alert 2: 0.750
        Assert.Equal(0.750, presentation.CalculateOpacity(skipCount: 0, opportunityIndex: 2), precision: 3);

        // Alert 3: 0.875
        Assert.Equal(0.875, presentation.CalculateOpacity(skipCount: 0, opportunityIndex: 3), precision: 3);

        // Alert 4: Max opacity (1.00)
        Assert.Equal(1.000, presentation.CalculateOpacity(skipCount: 0, opportunityIndex: 4), precision: 3);

        // Alert 5+: Clamped at 1.00
        Assert.Equal(1.000, presentation.CalculateOpacity(skipCount: 0, opportunityIndex: 5), precision: 3);
    }

    [Fact]
    public void FullPipelineIntegration_WithNotificationEngine_ScalesOpacityPerAlert()
    {
        var history = new InMemoryNotificationHistory();
        var presentation = new NotificationPresentationPolicy();
        var engine = new NotificationEngine(
            _policy,
            new NotificationSuppressionPolicy(),
            history,
            presentation);

        var deadline = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2);
        var startTime = deadline - interval;
        var task = new ListitTask("Pipeline Task", startTime: startTime, interval: interval, urgency: 4);
        var occurrence = new TaskOccurrence(task.Id, deadline);

        // 1. Time at 50% (11:00) -> Alert 0 fires with Opacity 0.50
        var ctx50 = new NotificationContext(task, occurrence, startTime.AddMinutes(60), SchedulingState.Upcoming);
        var decision0 = engine.Evaluate(ctx50);
        Assert.True(decision0.ShouldNotify);
        Assert.Equal(0.50, decision0.Opacity, precision: 3);
        Assert.True(history.HasBeenEmitted(occurrence.OccurrenceId, 0));

        // 2. Re-evaluating at 11:05 -> Idempotent, does not re-notify
        var ctx55 = new NotificationContext(task, occurrence, startTime.AddMinutes(65), SchedulingState.Upcoming);
        var decisionIdempotent = engine.Evaluate(ctx55);
        Assert.False(decisionIdempotent.ShouldNotify);

        // 3. Time at 75% (11:30) -> Alert 1 fires with Opacity 0.625
        var ctx75 = new NotificationContext(task, occurrence, startTime.AddMinutes(90), SchedulingState.Upcoming);
        var decision1 = engine.Evaluate(ctx75);
        Assert.True(decision1.ShouldNotify);
        Assert.Equal(0.625, decision1.Opacity, precision: 3);
        Assert.True(history.HasBeenEmitted(occurrence.OccurrenceId, 1));

        // 4. Time at 87.5% (11:45) -> Alert 2 fires with Opacity 0.75
        var ctx87 = new NotificationContext(task, occurrence, startTime.AddMinutes(105), SchedulingState.Upcoming);
        var decision2 = engine.Evaluate(ctx87);
        Assert.True(decision2.ShouldNotify);
        Assert.Equal(0.750, decision2.Opacity, precision: 3);
        Assert.True(history.HasBeenEmitted(occurrence.OccurrenceId, 2));
    }
}
