using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Immutable context describing an action performed on a notification.
/// Carries task and occurrence identifiers, the specific action taken, and evaluation metadata.
/// </summary>
public class NotificationActionContext
{
    public Guid TaskId { get; }
    public Guid OccurrenceId { get; }
    public NotificationAction Action { get; }
    public int? OpportunityIndex { get; }
    public DateTime Timestamp { get; }

    public NotificationActionContext(
        Guid taskId,
        Guid occurrenceId,
        NotificationAction action,
        int? opportunityIndex = null,
        DateTime? timestamp = null)
    {
        if (taskId == Guid.Empty)
        {
            throw new ArgumentException("Task ID cannot be empty.", nameof(taskId));
        }

        if (occurrenceId == Guid.Empty)
        {
            throw new ArgumentException("Occurrence ID cannot be empty.", nameof(occurrenceId));
        }

        TaskId = taskId;
        OccurrenceId = occurrenceId;
        Action = action;
        OpportunityIndex = opportunityIndex;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }
}
