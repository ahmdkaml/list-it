using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Platform-independent model representing the result of evaluating whether a notification should occur.
/// Contains notification output, completely decoupled from scheduler internals or UI platforms.
/// </summary>
public class NotificationDecision
{
    public bool ShouldNotify { get; }
    public Guid TaskId { get; }
    public Guid OccurrenceId { get; }
    public int Urgency { get; }
    public int SkipCount { get; }
    public TimeSpan Elapsed { get; }
    public TimeSpan Remaining { get; }
    public NotificationVisualCategory VisualCategory { get; }
    public double Opacity { get; }

    public NotificationDecision(
        bool shouldNotify,
        Guid taskId,
        Guid occurrenceId,
        int urgency,
        int skipCount,
        TimeSpan elapsed,
        TimeSpan remaining,
        NotificationVisualCategory visualCategory,
        double opacity)
    {
        ShouldNotify = shouldNotify;
        TaskId = taskId;
        OccurrenceId = occurrenceId;
        Urgency = urgency;
        SkipCount = skipCount;
        Elapsed = elapsed;
        Remaining = remaining;
        VisualCategory = visualCategory;
        Opacity = opacity;
    }

    /// <summary>
    /// Creates a decision indicating no notification should be presented.
    /// </summary>
    public static NotificationDecision DoNotNotify(
        Guid taskId,
        Guid occurrenceId,
        int urgency = 1,
        int skipCount = 0,
        TimeSpan elapsed = default,
        TimeSpan remaining = default,
        NotificationVisualCategory visualCategory = NotificationVisualCategory.Low,
        double opacity = 0.0)
    {
        return new NotificationDecision(
            shouldNotify: false,
            taskId: taskId,
            occurrenceId: occurrenceId,
            urgency: urgency,
            skipCount: skipCount,
            elapsed: elapsed,
            remaining: remaining,
            visualCategory: visualCategory,
            opacity: opacity);
    }

    /// <summary>
    /// Creates a decision indicating a notification should be presented with the specified parameters.
    /// </summary>
    public static NotificationDecision Notify(
        Guid taskId,
        Guid occurrenceId,
        int urgency,
        int skipCount,
        TimeSpan elapsed,
        TimeSpan remaining,
        NotificationVisualCategory visualCategory,
        double opacity = 1.0)
    {
        return new NotificationDecision(
            shouldNotify: true,
            taskId: taskId,
            occurrenceId: occurrenceId,
            urgency: urgency,
            skipCount: skipCount,
            elapsed: elapsed,
            remaining: remaining,
            visualCategory: visualCategory,
            opacity: opacity);
    }
}
