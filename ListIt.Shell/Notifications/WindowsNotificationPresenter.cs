using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using ListIt.Core.Notifications;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Windows-specific notification delivery adapter.
/// Translates platform-independent NotificationDecision models into presentation requests,
/// isolates UI-thread dispatch, and protects the application runtime against presentation failures.
/// </summary>
public class WindowsNotificationPresenter : INotificationPresenter
{
    private readonly Action<Action> _uiDispatcher;
    private readonly Action<NotificationPresentationRequest>? _onDisplayRequested;
    private readonly List<NotificationPresentationRequest> _activeRequests = new();
    private readonly object _lock = new();

    public IReadOnlyList<NotificationPresentationRequest> ActiveRequests
    {
        get
        {
            lock (_lock)
            {
                return _activeRequests.ToArray();
            }
        }
    }

    public WindowsNotificationPresenter(
        Action<Action>? uiDispatcher = null,
        Action<NotificationPresentationRequest>? onDisplayRequested = null)
    {
        _uiDispatcher = uiDispatcher ?? (action =>
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        });

        _onDisplayRequested = onDisplayRequested;
    }

    public void Present(NotificationDecision decision)
    {
        if (decision == null) throw new ArgumentNullException(nameof(decision));

        // Ignore decisions that are not eligible for notification presentation
        if (!decision.ShouldNotify)
        {
            return;
        }

        try
        {
            var request = NotificationPresentationRequest.FromDecision(decision);

            _uiDispatcher(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        _activeRequests.Add(request);
                    }

                    _onDisplayRequested?.Invoke(request);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to display notification for Task {request.TaskId}: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to dispatch notification presentation for Task {decision.TaskId}: {ex}");
        }
    }
}
