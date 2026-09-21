namespace ListIt.Core.Notifications;

/// <summary>
/// Core contract for evaluating notification decisions from a supplied notification context.
/// </summary>
public interface INotificationEngine
{
    NotificationDecision Evaluate(NotificationContext context);
}
