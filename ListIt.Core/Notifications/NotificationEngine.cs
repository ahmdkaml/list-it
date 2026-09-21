using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Pipeline orchestrator that coordinates timing policy evaluation, opportunity discovery,
/// priority suppression, and decision production.
/// </summary>
public class NotificationEngine : INotificationEngine
{
    private readonly INotificationTimingPolicy _timingPolicy;
    private readonly INotificationSuppressionPolicy _suppressionPolicy;
    private readonly INotificationHistory _history;

    public NotificationEngine(
        INotificationTimingPolicy timingPolicy,
        INotificationSuppressionPolicy suppressionPolicy,
        INotificationHistory history)
    {
        _timingPolicy = timingPolicy ?? throw new ArgumentNullException(nameof(timingPolicy));
        _suppressionPolicy = suppressionPolicy ?? throw new ArgumentNullException(nameof(suppressionPolicy));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public NotificationDecision Evaluate(NotificationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var elapsed = context.CurrentTime > context.ScheduledAt
            ? context.CurrentTime - context.ScheduledAt
            : TimeSpan.Zero;

        // 1. Evaluate time-based eligibility
        var timingResult = _timingPolicy.Evaluate(context, _history);
        if (!timingResult.ShouldNotify || timingResult.EligibleOpportunities.Count == 0)
        {
            return NotificationDecision.DoNotNotify(
                taskId: context.TaskId,
                occurrenceId: context.OccurrenceId,
                urgency: context.Urgency,
                skipCount: context.SkipCount,
                elapsed: elapsed);
        }

        NotificationSuppressionResult? lastSuppressionResult = null;
        bool anyAllowed = false;

        // 2. Evaluate priority suppression for each eligible opportunity
        foreach (var opportunity in timingResult.EligibleOpportunities)
        {
            var suppression = _suppressionPolicy.Evaluate(context, opportunity);
            lastSuppressionResult = suppression;

            // Invariant: Both allowed and suppressed opportunities are marked as emitted
            // in history so suppressed opportunities do not flood retroactively when active work ends.
            _history.RecordEmitted(opportunity.OccurrenceId, opportunity.OpportunityIndex);

            if (suppression.IsAllowed)
            {
                anyAllowed = true;
            }
        }

        if (anyAllowed)
        {
            return NotificationDecision.Notify(
                taskId: context.TaskId,
                occurrenceId: context.OccurrenceId,
                urgency: context.Urgency,
                skipCount: context.SkipCount,
                elapsed: elapsed,
                remaining: TimeSpan.Zero,
                visualCategory: NotificationVisualCategory.Low,
                opacity: 1.0,
                suppressionResult: lastSuppressionResult);
        }

        return NotificationDecision.DoNotNotify(
            taskId: context.TaskId,
            occurrenceId: context.OccurrenceId,
            urgency: context.Urgency,
            skipCount: context.SkipCount,
            elapsed: elapsed,
            remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.Low,
            opacity: 0.0,
            suppressionResult: lastSuppressionResult);
    }
}
