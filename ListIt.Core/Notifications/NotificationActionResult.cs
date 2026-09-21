namespace ListIt.Core.Notifications;

/// <summary>
/// Status outcome of executing a notification action.
/// </summary>
public enum NotificationActionStatus
{
    /// <summary>
    /// Action executed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Target task or occurrence was not found (e.g. already deleted).
    /// </summary>
    NotFound,

    /// <summary>
    /// Target occurrence was in an invalid state for the requested action (e.g. already completed or missed).
    /// </summary>
    InvalidState,

    /// <summary>
    /// Action failed due to an unexpected error.
    /// </summary>
    Failed
}

/// <summary>
/// Result returned after processing a notification action.
/// Communicates whether the popup should close, whether application state changed, and diagnostic details.
/// </summary>
public class NotificationActionResult
{
    public NotificationActionStatus Status { get; }
    public bool ShouldClosePopup { get; }
    public bool StateChanged { get; }
    public string? Message { get; }

    public bool IsSuccess => Status == NotificationActionStatus.Success;

    public NotificationActionResult(
        NotificationActionStatus status,
        bool shouldClosePopup,
        bool stateChanged,
        string? message = null)
    {
        Status = status;
        ShouldClosePopup = shouldClosePopup;
        StateChanged = stateChanged;
        Message = message;
    }

    public static NotificationActionResult Success(
        bool shouldClosePopup = true,
        bool stateChanged = true,
        string? message = null) =>
        new(NotificationActionStatus.Success, shouldClosePopup, stateChanged, message);

    public static NotificationActionResult NotFound(
        string message,
        bool shouldClosePopup = true) =>
        new(NotificationActionStatus.NotFound, shouldClosePopup, stateChanged: false, message);

    public static NotificationActionResult InvalidState(
        string message,
        bool shouldClosePopup = true) =>
        new(NotificationActionStatus.InvalidState, shouldClosePopup, stateChanged: false, message);

    public static NotificationActionResult Fail(
        string message,
        bool shouldClosePopup = true) =>
        new(NotificationActionStatus.Failed, shouldClosePopup, stateChanged: false, message);
}
