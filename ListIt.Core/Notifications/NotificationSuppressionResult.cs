using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Encapsulates the outcome of evaluating priority suppression for a notification opportunity.
/// Decoupled from UI and delivery mechanisms.
/// </summary>
public class NotificationSuppressionResult
{
    public bool IsAllowed { get; }
    public string Reason { get; }
    public Guid? WorkingTaskId { get; }
    public int? WorkingTaskUrgency { get; }

    public NotificationSuppressionResult(
        bool isAllowed,
        string reason,
        Guid? workingTaskId = null,
        int? workingTaskUrgency = null)
    {
        IsAllowed = isAllowed;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        WorkingTaskId = workingTaskId;
        WorkingTaskUrgency = workingTaskUrgency;
    }

    public static NotificationSuppressionResult Allowed(
        string reason,
        Guid? workingTaskId = null,
        int? workingTaskUrgency = null) =>
        new(true, reason, workingTaskId, workingTaskUrgency);

    public static NotificationSuppressionResult Suppressed(
        string reason,
        Guid? workingTaskId = null,
        int? workingTaskUrgency = null) =>
        new(false, reason, workingTaskId, workingTaskUrgency);
}
