using System;
using ListIt.Shell.Notifications;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Abstraction for delivering and managing native Windows Toast notifications in the Windows Action Center.
/// Strictly isolated to the ListIt.Shell presentation layer.
/// </summary>
public interface IWindowsToastNotifier : IDisposable
{
    /// <summary>
    /// Displays a native Windows Toast notification in the Windows Notification / Action Center.
    /// </summary>
    /// <param name="request">The notification presentation request details.</param>
    void ShowToast(NotificationPresentationRequest request);

    /// <summary>
    /// Removes a specific toast notification from the Windows Action Center by its tag and group.
    /// </summary>
    /// <param name="tag">The unique tag of the toast (typically the OccurrenceId string).</param>
    /// <param name="group">The group category of the toast, or null for default group.</param>
    void RemoveToast(string tag, string? group = null);

    /// <summary>
    /// Clears all toast notifications posted by this application from the Windows Action Center.
    /// </summary>
    void ClearToasts();

    /// <summary>
    /// Event raised when a toast notification is activated or an action button is clicked.
    /// </summary>
    event Action<string>? ToastActivated;
}
