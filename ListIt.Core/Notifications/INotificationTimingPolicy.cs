using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Evaluates whether an occurrence is eligible for time-based notification opportunities
/// based on its urgency, elapsed time, and lapse duration.
/// </summary>
public interface INotificationTimingPolicy
{
    NotificationTimingResult Evaluate(
        NotificationContext context,
        INotificationHistory history,
        TimeSpan? lapseDuration = null);
}
