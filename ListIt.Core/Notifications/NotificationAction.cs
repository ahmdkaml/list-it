namespace ListIt.Core.Notifications;

/// <summary>
/// Defines the set of user actions that can be performed on a notification popup.
/// </summary>
public enum NotificationAction
{
    /// <summary>
    /// Indicates the user is actively working on the task/occurrence now.
    /// </summary>
    Work,

    /// <summary>
    /// Indicates the user completed the task/occurrence.
    /// </summary>
    Done,

    /// <summary>
    /// Indicates the user wants to dismiss the popup without completing or modifying occurrence state.
    /// </summary>
    Dismiss
}
