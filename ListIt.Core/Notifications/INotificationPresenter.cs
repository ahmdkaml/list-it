using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Application-level abstraction for presenting an evaluated notification decision.
/// Completely decoupled from UI frameworks, window handles, and operating system APIs.
/// </summary>
public interface INotificationPresenter
{
    void Present(NotificationDecision decision);
}
