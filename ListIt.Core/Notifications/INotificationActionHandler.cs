namespace ListIt.Core.Notifications;

/// <summary>
/// Application boundary contract for handling user interactions from notification presentations.
/// Translates notification actions into application operations without coupling the presentation UI to domain repositories.
/// </summary>
public interface INotificationActionHandler
{
    /// <summary>
    /// Processes a notification action within the application domain.
    /// </summary>
    /// <param name="context">The context describing the action, occurrence, and task.</param>
    /// <returns>A NotificationActionResult indicating the outcome and whether the popup should close.</returns>
    NotificationActionResult Handle(NotificationActionContext context);
}
