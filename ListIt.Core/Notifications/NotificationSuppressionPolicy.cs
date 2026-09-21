using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Evaluates whether a notification opportunity is allowed or suppressed based on active work and urgency priorities.
/// The candidate task's own BypassPrioritySuppression property controls whether it bypasses suppression.
/// The currently working task's BypassPrioritySuppression setting has no effect on candidate suppression.
/// </summary>
public class NotificationSuppressionPolicy : INotificationSuppressionPolicy
{
    public const string ReasonAllowedNoActiveWork = "AllowedNoActiveWork";
    public const string ReasonAllowedBypassPrioritySuppression = "AllowedBypassPrioritySuppression";
    public const string ReasonAllowedHigherOrEqualPriority = "AllowedHigherOrEqualPriority";
    public const string ReasonSuppressedByPriority = "SuppressedByPriority";

    public NotificationSuppressionResult Evaluate(NotificationContext context, NotificationOpportunity? opportunity = null)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        // 1. If no task is currently actively being worked on, notification is allowed
        if (context.ActiveWorkingTask == null)
        {
            return NotificationSuppressionResult.Allowed(ReasonAllowedNoActiveWork);
        }

        var workingTaskId = context.ActiveWorkingTask.Id;
        var workingTaskUrgency = context.ActiveWorkingTask.Urgency;

        // 2. The candidate task's own BypassPrioritySuppression setting bypasses suppression
        if (context.BypassPrioritySuppression)
        {
            return NotificationSuppressionResult.Allowed(
                ReasonAllowedBypassPrioritySuppression,
                workingTaskId,
                workingTaskUrgency);
        }

        // 3. If candidate urgency is greater than or equal to the active working task's urgency, it is allowed
        if (context.Urgency >= workingTaskUrgency)
        {
            return NotificationSuppressionResult.Allowed(
                ReasonAllowedHigherOrEqualPriority,
                workingTaskId,
                workingTaskUrgency);
        }

        // 4. Otherwise, candidate urgency is strictly lower than the working task's urgency, so it is suppressed
        return NotificationSuppressionResult.Suppressed(
            ReasonSuppressedByPriority,
            workingTaskId,
            workingTaskUrgency);
    }
}
