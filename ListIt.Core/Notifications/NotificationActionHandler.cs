using System;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;

namespace ListIt.Core.Notifications;

/// <summary>
/// Handles user notification actions (Work, Done, Dismiss) at the application layer.
/// Coordinates with ISchedulingRuntime and ITaskService, protects against double-clicks/race conditions,
/// and handles stale or deleted tasks safely without throwing unhandled exceptions.
/// </summary>
public class NotificationActionHandler : INotificationActionHandler
{
    private readonly ISchedulingRuntime _schedulingRuntime;
    private readonly ITaskService _taskService;
    private readonly INotificationHistory? _history;
    private readonly object _lock = new();

    public NotificationActionHandler(
        ISchedulingRuntime schedulingRuntime,
        ITaskService taskService,
        INotificationHistory? history = null)
    {
        _schedulingRuntime = schedulingRuntime ?? throw new ArgumentNullException(nameof(schedulingRuntime));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _history = history;
    }

    public NotificationActionResult Handle(NotificationActionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        lock (_lock)
        {
            try
            {
                return context.Action switch
                {
                    NotificationAction.Dismiss => HandleDismiss(context),
                    NotificationAction.Work => HandleWork(context),
                    NotificationAction.Done => HandleDone(context),
                    _ => NotificationActionResult.Fail($"Unsupported notification action '{context.Action}'.", shouldClosePopup: true)
                };
            }
            catch (Exception ex)
            {
                return NotificationActionResult.Fail($"Failed to execute action '{context.Action}': {ex.Message}", shouldClosePopup: true);
            }
        }
    }

    private NotificationActionResult HandleDismiss(NotificationActionContext context)
    {
        if (_history != null && context.OpportunityIndex.HasValue)
        {
            _history.RecordEmitted(context.OccurrenceId, context.OpportunityIndex.Value);
        }

        return NotificationActionResult.Success(
            shouldClosePopup: true,
            stateChanged: false,
            message: "Notification dismissed.");
    }

    private NotificationActionResult HandleWork(NotificationActionContext context)
    {
        var task = _taskService.GetTask(context.TaskId);
        if (task == null)
        {
            return NotificationActionResult.NotFound(
                $"Task '{context.TaskId}' not found or was deleted.",
                shouldClosePopup: true);
        }

        var occurrence = _schedulingRuntime.GetOccurrence(context.OccurrenceId);
        if (occurrence == null)
        {
            return NotificationActionResult.NotFound(
                $"Occurrence '{context.OccurrenceId}' not found.",
                shouldClosePopup: true);
        }

        if (occurrence.TaskId != task.Id)
        {
            return NotificationActionResult.InvalidState(
                $"Occurrence '{context.OccurrenceId}' does not belong to task '{task.Id}'.",
                shouldClosePopup: true);
        }

        // Idempotency: Double-clicks on Work
        if (occurrence.IsWorking)
        {
            return NotificationActionResult.Success(
                shouldClosePopup: true,
                stateChanged: false,
                message: "Occurrence is already in working state.");
        }

        if (occurrence.Status != OccurrenceStatus.Pending)
        {
            return NotificationActionResult.InvalidState(
                $"Occurrence '{context.OccurrenceId}' has status '{occurrence.Status}' and cannot be worked on.",
                shouldClosePopup: true);
        }

        _schedulingRuntime.StartWorking(occurrence);

        return NotificationActionResult.Success(
            shouldClosePopup: true,
            stateChanged: true,
            message: "Work started on occurrence.");
    }

    private NotificationActionResult HandleDone(NotificationActionContext context)
    {
        var task = _taskService.GetTask(context.TaskId);
        if (task == null)
        {
            return NotificationActionResult.NotFound(
                $"Task '{context.TaskId}' not found or was deleted.",
                shouldClosePopup: true);
        }

        var occurrence = _schedulingRuntime.GetOccurrence(context.OccurrenceId);
        if (occurrence == null)
        {
            return NotificationActionResult.NotFound(
                $"Occurrence '{context.OccurrenceId}' not found.",
                shouldClosePopup: true);
        }

        if (occurrence.TaskId != task.Id)
        {
            return NotificationActionResult.InvalidState(
                $"Occurrence '{context.OccurrenceId}' does not belong to task '{task.Id}'.",
                shouldClosePopup: true);
        }

        // Idempotency: Double-clicks on Done
        if (occurrence.Status == OccurrenceStatus.Completed)
        {
            return NotificationActionResult.Success(
                shouldClosePopup: true,
                stateChanged: false,
                message: "Occurrence was already completed.");
        }

        if (occurrence.Status != OccurrenceStatus.Pending)
        {
            return NotificationActionResult.InvalidState(
                $"Occurrence '{context.OccurrenceId}' has status '{occurrence.Status}' and cannot be completed.",
                shouldClosePopup: true);
        }

        _schedulingRuntime.CompleteOccurrence(occurrence);

        return NotificationActionResult.Success(
            shouldClosePopup: true,
            stateChanged: true,
            message: "Occurrence completed successfully.");
    }
}
