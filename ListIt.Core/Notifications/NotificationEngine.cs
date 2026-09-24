using System;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;

namespace ListIt.Core.Notifications;

/// <summary>
/// Pipeline orchestrator that coordinates timing policy evaluation, opportunity discovery,
/// priority suppression, presentation policy, and decision production.
/// </summary>
public class NotificationEngine : INotificationEngine
{
    private readonly INotificationTimingPolicy _timingPolicy;
    private readonly INotificationSuppressionPolicy _suppressionPolicy;
    private readonly INotificationPresentationPolicy _presentationPolicy;
    private readonly INotificationHistory _history;

    public NotificationEngine(
        INotificationTimingPolicy timingPolicy,
        INotificationSuppressionPolicy suppressionPolicy,
        INotificationHistory history,
        INotificationPresentationPolicy? presentationPolicy = null)
    {
        _timingPolicy = timingPolicy ?? throw new ArgumentNullException(nameof(timingPolicy));
        _suppressionPolicy = suppressionPolicy ?? throw new ArgumentNullException(nameof(suppressionPolicy));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _presentationPolicy = presentationPolicy ?? new NotificationPresentationPolicy();
    }

    public NotificationDecision Evaluate(NotificationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var elapsed = context.CurrentTime > context.ScheduledAt
            ? context.CurrentTime - context.ScheduledAt
            : TimeSpan.Zero;

        var remaining = context.CurrentTime < context.ScheduledAt
            ? context.ScheduledAt - context.CurrentTime
            : TimeSpan.Zero;

        // Terminal occurrences never produce notification output
        if (context.Occurrence.Status is OccurrenceStatus.Completed or OccurrenceStatus.Missed)
        {
            return NotificationDecision.DoNotNotify(
                taskId: context.TaskId,
                occurrenceId: context.OccurrenceId,
                urgency: context.Urgency,
                skipCount: context.SkipCount,
                elapsed: elapsed,
                remaining: remaining,
                visualCategory: _presentationPolicy.GetVisualCategory(context.Urgency),
                opacity: 0.0,
                taskTitle: context.Task.Title);
        }

        // 1. Evaluate time-based eligibility
        var timingResult = _timingPolicy.Evaluate(context, _history);
        if (!timingResult.ShouldNotify || timingResult.EligibleOpportunities.Count == 0)
        {
            return NotificationDecision.DoNotNotify(
                taskId: context.TaskId,
                occurrenceId: context.OccurrenceId,
                urgency: context.Urgency,
                skipCount: context.SkipCount,
                elapsed: elapsed,
                remaining: remaining,
                visualCategory: _presentationPolicy.GetVisualCategory(context.Urgency),
                opacity: 0.0,
                opportunities: timingResult.EligibleOpportunities,
                taskTitle: context.Task.Title);
        }

        NotificationDecision? presentationDecision = null;
        NotificationSuppressionResult? lastSuppression = null;

        // 2. Evaluate priority suppression and presentation for each eligible opportunity
        foreach (var opportunity in timingResult.EligibleOpportunities)
        {
            var suppression = _suppressionPolicy.Evaluate(context, opportunity);
            lastSuppression = suppression;

            // Invariant: Both allowed and suppressed opportunities are marked as emitted
            // in history so suppressed opportunities do not flood retroactively when active work ends.
            _history.RecordEmitted(opportunity.OccurrenceId, opportunity.OpportunityIndex);

            var decision = _presentationPolicy.Evaluate(context, opportunity, suppression);
            if (decision.ShouldNotify && presentationDecision == null)
            {
                presentationDecision = decision;
            }
        }

        if (presentationDecision != null)
        {
            return new NotificationDecision(
                shouldNotify: true,
                taskId: presentationDecision.TaskId,
                occurrenceId: presentationDecision.OccurrenceId,
                urgency: presentationDecision.Urgency,
                skipCount: presentationDecision.SkipCount,
                elapsed: presentationDecision.Elapsed,
                remaining: presentationDecision.Remaining,
                visualCategory: presentationDecision.VisualCategory,
                opacity: presentationDecision.Opacity,
                suppressionResult: presentationDecision.SuppressionResult,
                opportunities: timingResult.EligibleOpportunities,
                taskTitle: context.Task.Title);
        }

        return NotificationDecision.DoNotNotify(
            taskId: context.TaskId,
            occurrenceId: context.OccurrenceId,
            urgency: context.Urgency,
            skipCount: context.SkipCount,
            elapsed: elapsed,
            remaining: remaining,
            visualCategory: _presentationPolicy.GetVisualCategory(context.Urgency),
            opacity: 0.0,
            suppressionResult: lastSuppression,
            opportunities: timingResult.EligibleOpportunities,
            taskTitle: context.Task.Title);
    }
}
