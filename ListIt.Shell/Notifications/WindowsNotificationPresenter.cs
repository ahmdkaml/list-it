using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using ListIt.Core.Notifications;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Windows-specific notification delivery adapter.
/// Translates platform-independent NotificationDecision models into presentation requests,
/// isolates UI-thread dispatch, manages bottom-right window positioning and vertical stacking,
/// routes interaction to INotificationActionHandler, and protects the application runtime against presentation failures.
/// </summary>
public class WindowsNotificationPresenter : INotificationPresenter
{
    private readonly Action<Action> _uiDispatcher;
    private readonly Action<NotificationPresentationRequest>? _onDisplayRequested;
    private readonly double _lifetimeSeconds;
    private readonly INotificationActionHandler? _actionHandler;
    private readonly IWindowsToastNotifier? _toastNotifier;
    private readonly List<NotificationPresentationRequest> _activeRequests = new();
    private readonly List<NotificationWindow> _activeWindows = new();
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

    public IReadOnlyList<NotificationWindow> ActiveWindows
    {
        get
        {
            lock (_lock)
            {
                return _activeWindows.ToArray();
            }
        }
    }

    public IWindowsToastNotifier? ToastNotifier => _toastNotifier;

    public WindowsNotificationPresenter(
        Action<Action>? uiDispatcher = null,
        Action<NotificationPresentationRequest>? onDisplayRequested = null,
        double lifetimeSeconds = NotificationWindow.DefaultLifetimeSeconds,
        INotificationActionHandler? actionHandler = null,
        IWindowsToastNotifier? toastNotifier = null)
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
        _lifetimeSeconds = lifetimeSeconds;
        _actionHandler = actionHandler;
        _toastNotifier = toastNotifier;
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

            if (_toastNotifier != null)
            {
                try
                {
                    _toastNotifier.ShowToast(request);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to display native toast notification for Task {request.TaskId}: {ex}");
                }
            }

            _uiDispatcher(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        _activeRequests.Add(request);
                    }

                    if (_onDisplayRequested != null)
                    {
                        _onDisplayRequested.Invoke(request);
                    }
                    else
                    {
                        DisplayWindow(request);
                    }
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

    public Task PresentAsync(NotificationDecision decision)
    {
        Present(decision);
        return Task.CompletedTask;
    }

    private void DisplayWindow(NotificationPresentationRequest request)
    {
        var effectiveActionHandler = (_toastNotifier != null && _actionHandler != null)
            ? new ActionHandlerToastDecorator(_actionHandler, _toastNotifier)
            : _actionHandler;

        var viewModel = new NotificationViewModel(request, effectiveActionHandler);
        var window = new NotificationWindow(viewModel, _lifetimeSeconds);

        lock (_lock)
        {
            _activeWindows.Add(window);
        }

        window.Loaded += (s, e) => ReflowWindows();

        window.Closed += (s, e) =>
        {
            lock (_lock)
            {
                _activeWindows.Remove(window);
                _activeRequests.Remove(request);
            }
            ReflowWindows();
        };

        // Initial approximate position before Loaded
        var workArea = SystemParameters.WorkArea;
        var approxSize = new Size(320, 160);
        int currentIndex;
        lock (_lock)
        {
            currentIndex = Math.Max(0, _activeWindows.Count - 1);
        }
        var initialPos = NotificationPositioningService.CalculatePosition(workArea, approxSize, currentIndex);
        window.Left = initialPos.X;
        window.Top = initialPos.Y;

        window.Show();
    }

    private void ReflowWindows()
    {
        var workArea = SystemParameters.WorkArea;
        lock (_lock)
        {
            for (int i = 0; i < _activeWindows.Count; i++)
            {
                var win = _activeWindows[i];
                var actualHeight = win.ActualHeight > 0 ? win.ActualHeight : 160;
                var actualWidth = win.ActualWidth > 0 ? win.ActualWidth : 320;
                var pos = NotificationPositioningService.CalculatePosition(workArea, new Size(actualWidth, actualHeight), i);
                win.Left = pos.X;
                win.Top = pos.Y;
            }
        }
    }

    private class ActionHandlerToastDecorator : INotificationActionHandler
    {
        private readonly INotificationActionHandler _innerHandler;
        private readonly IWindowsToastNotifier _toastNotifier;

        public ActionHandlerToastDecorator(INotificationActionHandler innerHandler, IWindowsToastNotifier toastNotifier)
        {
            _innerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
            _toastNotifier = toastNotifier ?? throw new ArgumentNullException(nameof(toastNotifier));
        }

        public NotificationActionResult Handle(NotificationActionContext context)
        {
            var result = _innerHandler.Handle(context);
            if (context.Action == NotificationAction.Done || context.Action == NotificationAction.Dismiss)
            {
                _toastNotifier.RemoveToast(context.OccurrenceId.ToString());
            }
            return result;
        }
    }
}
