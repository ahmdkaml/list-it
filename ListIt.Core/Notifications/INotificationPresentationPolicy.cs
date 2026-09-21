using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Contract for converting notification context and suppression outcomes into platform-independent presentation decisions.
/// Completely decoupled from UI frameworks, window handles, and color definitions.
/// </summary>
public interface INotificationPresentationPolicy
{
    NotificationDecision Evaluate(
        NotificationContext context,
        NotificationOpportunity opportunity,
        NotificationSuppressionResult suppression);

    NotificationVisualCategory GetVisualCategory(int urgency);

    double CalculateOpacity(int skipCount);
}
