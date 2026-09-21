using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Represents a discrete, uniquely identifiable notification opportunity during an occurrence's lapse.
/// Provides a stable logical identity to ensure idempotency across repeated evaluations.
/// </summary>
public class NotificationOpportunity
{
    public Guid TaskId { get; }
    public Guid OccurrenceId { get; }
    public int OpportunityIndex { get; }
    public double Threshold { get; }
    public DateTime ThresholdTime { get; }

    /// <summary>
    /// Stable logical key uniquely identifying this opportunity across repeated evaluation passes.
    /// </summary>
    public string LogicalKey => $"{OccurrenceId:D}_{OpportunityIndex}";

    public NotificationOpportunity(
        Guid taskId,
        Guid occurrenceId,
        int opportunityIndex,
        double threshold,
        DateTime thresholdTime)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
        if (occurrenceId == Guid.Empty) throw new ArgumentException("OccurrenceId cannot be empty.", nameof(occurrenceId));
        if (opportunityIndex < 0) throw new ArgumentOutOfRangeException(nameof(opportunityIndex), opportunityIndex, "Opportunity index cannot be negative.");

        TaskId = taskId;
        OccurrenceId = occurrenceId;
        OpportunityIndex = opportunityIndex;
        Threshold = threshold;
        ThresholdTime = thresholdTime;
    }
}
