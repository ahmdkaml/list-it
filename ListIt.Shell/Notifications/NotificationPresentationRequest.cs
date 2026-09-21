using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Notifications;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Structured presentation model carrying notification content to the Windows presentation layer.
/// Decoupled from concrete XAML/WPF controls.
/// </summary>
public class NotificationPresentationRequest
{
    public Guid TaskId { get; }
    public Guid OccurrenceId { get; }
    public int Urgency { get; }
    public int SkipCount { get; }
    public TimeSpan Elapsed { get; }
    public TimeSpan Remaining { get; }
    public NotificationVisualCategory VisualCategory { get; }
    public double Opacity { get; }
    public IReadOnlyList<NotificationOpportunity> Opportunities { get; }

    public NotificationPresentationRequest(
        Guid taskId,
        Guid occurrenceId,
        int urgency,
        int skipCount,
        TimeSpan elapsed,
        TimeSpan remaining,
        NotificationVisualCategory visualCategory,
        double opacity,
        IEnumerable<NotificationOpportunity>? opportunities = null)
    {
        TaskId = taskId;
        OccurrenceId = occurrenceId;
        Urgency = urgency;
        SkipCount = skipCount;
        Elapsed = elapsed;
        Remaining = remaining;
        VisualCategory = visualCategory;
        Opacity = Math.Clamp(opacity, 0.0, 1.0);
        Opportunities = (opportunities?.ToList() ?? new List<NotificationOpportunity>()).AsReadOnly();
    }

    /// <summary>
    /// Creates a structured presentation request from an evaluated NotificationDecision.
    /// Clamps opacity defensively to [0.0, 1.0].
    /// </summary>
    public static NotificationPresentationRequest FromDecision(NotificationDecision decision)
    {
        if (decision == null) throw new ArgumentNullException(nameof(decision));

        return new NotificationPresentationRequest(
            decision.TaskId,
            decision.OccurrenceId,
            decision.Urgency,
            decision.SkipCount,
            decision.Elapsed,
            decision.Remaining,
            decision.VisualCategory,
            decision.Opacity,
            decision.Opportunities);
    }
}
