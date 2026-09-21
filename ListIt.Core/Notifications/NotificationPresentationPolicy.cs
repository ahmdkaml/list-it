using System;
using ListIt.Core.Models;

namespace ListIt.Core.Notifications;

/// <summary>
/// Platform-independent presentation policy converting notification state and suppression outcomes
/// into semantic presentation metadata (visual category, opacity, elapsed/remaining time).
/// Free from any UI frameworks, color codes, or window dependencies.
/// </summary>
public class NotificationPresentationPolicy : INotificationPresentationPolicy
{
    public const double BaselineOpacity = 0.5;
    public const double MaxOpacity = 1.0;
    public const double OpacityStepPerSkip = 0.125;

    public NotificationDecision Evaluate(
        NotificationContext context,
        NotificationOpportunity opportunity,
        NotificationSuppressionResult suppression)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (opportunity == null) throw new ArgumentNullException(nameof(opportunity));
        if (suppression == null) throw new ArgumentNullException(nameof(suppression));

        var visualCategory = GetVisualCategory(context.Urgency);
        var opacity = CalculateOpacity(context.SkipCount);

        var elapsed = context.CurrentTime > context.ScheduledAt
            ? context.CurrentTime - context.ScheduledAt
            : TimeSpan.Zero;

        var remaining = context.CurrentTime < context.ScheduledAt
            ? context.ScheduledAt - context.CurrentTime
            : TimeSpan.Zero;

        // Terminal occurrences never produce normal notification presentation
        if (context.Occurrence.Status is OccurrenceStatus.Completed or OccurrenceStatus.Missed)
        {
            return NotificationDecision.DoNotNotify(
                taskId: context.TaskId,
                occurrenceId: context.OccurrenceId,
                urgency: context.Urgency,
                skipCount: context.SkipCount,
                elapsed: elapsed,
                remaining: remaining,
                visualCategory: visualCategory,
                opacity: 0.0,
                suppressionResult: suppression);
        }

        // Suppressed notifications do not produce an emit-ready presentation decision
        if (!suppression.IsAllowed)
        {
            return NotificationDecision.DoNotNotify(
                taskId: context.TaskId,
                occurrenceId: context.OccurrenceId,
                urgency: context.Urgency,
                skipCount: context.SkipCount,
                elapsed: elapsed,
                remaining: remaining,
                visualCategory: visualCategory,
                opacity: 0.0,
                suppressionResult: suppression);
        }

        return NotificationDecision.Notify(
            taskId: context.TaskId,
            occurrenceId: context.OccurrenceId,
            urgency: context.Urgency,
            skipCount: context.SkipCount,
            elapsed: elapsed,
            remaining: remaining,
            visualCategory: visualCategory,
            opacity: opacity,
            suppressionResult: suppression);
    }

    public NotificationVisualCategory GetVisualCategory(int urgency)
    {
        return urgency switch
        {
            1 or 2 => NotificationVisualCategory.Low,
            3 or 4 => NotificationVisualCategory.Medium,
            5 or 6 => NotificationVisualCategory.High,
            _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, "Urgency must be between 1 and 6.")
        };
    }

    public double CalculateOpacity(int skipCount)
    {
        if (skipCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skipCount), skipCount, "Skip count cannot be negative.");
        }

        if (skipCount <= 1)
        {
            return BaselineOpacity;
        }

        var calculated = BaselineOpacity + (skipCount - 1) * OpacityStepPerSkip;
        return Math.Clamp(calculated, BaselineOpacity, MaxOpacity);
    }
}
