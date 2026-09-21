using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Contract for evaluating priority-based notification suppression.
/// </summary>
public interface INotificationSuppressionPolicy
{
    NotificationSuppressionResult Evaluate(NotificationContext context, NotificationOpportunity? opportunity = null);
}
