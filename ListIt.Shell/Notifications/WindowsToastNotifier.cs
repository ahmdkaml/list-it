using System;
using System.Diagnostics;
using ListIt.Core.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Native Windows Toast notification service.
/// Uses Microsoft.Toolkit.Uwp.Notifications to deliver interactive notifications to the Windows Action Center,
/// listen for background or user activations, and route actions into the application domain.
/// </summary>
public class WindowsToastNotifier : IWindowsToastNotifier
{
    public const string DefaultToastGroup = "ListItTasks";

    private readonly INotificationActionHandler? _actionHandler;
    private readonly Action? _onActivateApplication;
    private bool _disposed;

    public event Action<string>? ToastActivated;

    public WindowsToastNotifier(
        INotificationActionHandler? actionHandler = null,
        Action? onActivateApplication = null)
    {
        _actionHandler = actionHandler;
        _onActivateApplication = onActivateApplication;

        try
        {
            ToastNotificationManagerCompat.OnActivated += HandleToastActivated;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Failed to subscribe to ToastNotificationManagerCompat.OnActivated: {ex.Message}");
        }
    }

    public virtual void ShowToast(NotificationPresentationRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        try
        {
            var tag = request.OccurrenceId.ToString();
            var opportunityIndex = request.Opportunities.FirstOrDefault()?.OpportunityIndex;

            var builder = new ToastContentBuilder()
                .AddArgument("action", "open")
                .AddArgument("taskId", request.TaskId.ToString())
                .AddArgument("occurrenceId", request.OccurrenceId.ToString());

            if (opportunityIndex.HasValue)
            {
                builder.AddArgument("opportunityIndex", opportunityIndex.Value.ToString());
            }

            var title = string.IsNullOrWhiteSpace(request.TaskTitle) ? "Untitled Task" : request.TaskTitle;
            builder.AddText(title);

            string subtitle = $"{request.VisualCategory} • Remaining: {NotificationTimeFormatter.Format(request.Remaining)}";
            builder.AddText(subtitle);

            // Action Buttons: Work, Done, Dismiss
            builder.AddButton(new ToastButton()
                .SetContent("Work")
                .AddArgument("action", "work")
                .AddArgument("taskId", request.TaskId.ToString())
                .AddArgument("occurrenceId", request.OccurrenceId.ToString())
                .SetBackgroundActivation());

            builder.AddButton(new ToastButton()
                .SetContent("Done")
                .AddArgument("action", "done")
                .AddArgument("taskId", request.TaskId.ToString())
                .AddArgument("occurrenceId", request.OccurrenceId.ToString())
                .SetBackgroundActivation());

            builder.AddButton(new ToastButton()
                .SetContent("Dismiss")
                .AddArgument("action", "dismiss")
                .AddArgument("taskId", request.TaskId.ToString())
                .AddArgument("occurrenceId", request.OccurrenceId.ToString())
                .SetBackgroundActivation());

            builder.Show(toast =>
            {
                toast.Tag = tag;
                toast.Group = DefaultToastGroup;
            });
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to display native toast notification for Task {request.TaskId}: {ex}");
        }
    }

    public virtual void RemoveToast(string tag, string? group = null)
    {
        try
        {
            ToastNotificationManagerCompat.History.Remove(tag, group ?? DefaultToastGroup);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Failed to remove toast notification '{tag}': {ex.Message}");
        }
    }

    public virtual void ClearToasts()
    {
        try
        {
            ToastNotificationManagerCompat.History.Clear();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Failed to clear toast notifications: {ex.Message}");
        }
    }

    public void ProcessActivationArgument(string argumentString)
    {
        if (string.IsNullOrWhiteSpace(argumentString)) return;

        ToastActivated?.Invoke(argumentString);

        try
        {
            var normalizedArgs = argumentString.Contains('&') && !argumentString.Contains(';')
                ? argumentString.Replace('&', ';')
                : argumentString;

            var args = ToastArguments.Parse(normalizedArgs);
            if (!args.TryGetValue("action", out var actionStr))
            {
                actionStr = "open";
            }

            if (string.Equals(actionStr, "open", StringComparison.OrdinalIgnoreCase))
            {
                _onActivateApplication?.Invoke();
                return;
            }

            if (!args.TryGetValue("taskId", out var taskIdStr) || !Guid.TryParse(taskIdStr, out var taskId))
            {
                return;
            }

            if (!args.TryGetValue("occurrenceId", out var occIdStr) || !Guid.TryParse(occIdStr, out var occId))
            {
                return;
            }

            int? opportunityIndex = null;
            if (args.TryGetValue("opportunityIndex", out var oppStr) && int.TryParse(oppStr, out var oppIdx))
            {
                opportunityIndex = oppIdx;
            }

            NotificationAction? action = actionStr.ToLowerInvariant() switch
            {
                "work" => NotificationAction.Work,
                "done" => NotificationAction.Done,
                "dismiss" => NotificationAction.Dismiss,
                _ => null
            };

            if (action.HasValue && _actionHandler != null)
            {
                var context = new NotificationActionContext(
                    taskId: taskId,
                    occurrenceId: occId,
                    action: action.Value,
                    opportunityIndex: opportunityIndex);

                _actionHandler.Handle(context);

                if (action.Value == NotificationAction.Done || action.Value == NotificationAction.Dismiss)
                {
                    RemoveToast(occId.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error handling toast activation argument '{argumentString}': {ex}");
        }
    }

    private void HandleToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        ProcessActivationArgument(e.Argument);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            ToastNotificationManagerCompat.OnActivated -= HandleToastActivated;
        }
        catch
        {
            // Ignore on cleanup
        }
    }
}
