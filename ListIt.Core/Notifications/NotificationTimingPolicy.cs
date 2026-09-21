using System;
using System.Collections.Generic;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;

namespace ListIt.Core.Notifications;

/// <summary>
/// Deterministic time-based notification policy implementing urgency frequency thresholds over a lapse interval.
/// Completely decoupled from priority suppression, UI rendering, and system clocks.
/// </summary>
public class NotificationTimingPolicy : INotificationTimingPolicy
{
    private readonly IReadOnlyDictionary<int, NotificationFrequencyPolicy> _frequencyPolicies;
    private readonly TimeSpan _defaultLapseDuration;

    public NotificationTimingPolicy(
        IReadOnlyDictionary<int, NotificationFrequencyPolicy>? frequencyPolicies = null,
        TimeSpan? defaultLapseDuration = null)
    {
        _frequencyPolicies = frequencyPolicies ?? NotificationFrequencyPolicy.GetDefaultPolicies();
        _defaultLapseDuration = defaultLapseDuration ?? TimeSpan.FromHours(1);

        if (_defaultLapseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultLapseDuration), "Default lapse duration must be greater than zero.");
        }
    }

    public NotificationTimingResult Evaluate(
        NotificationContext context,
        INotificationHistory history,
        TimeSpan? lapseDuration = null)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (history == null) throw new ArgumentNullException(nameof(history));

        // 1. Terminal occurrences never emit notifications
        if (context.Occurrence.Status is OccurrenceStatus.Completed or OccurrenceStatus.Missed)
        {
            return NotificationTimingResult.Empty;
        }

        // 2. Upcoming occurrences or occurrences whose scheduled time has not yet arrived never notify
        if (context.SchedulingState == SchedulingState.Upcoming || context.CurrentTime < context.ScheduledAt)
        {
            return NotificationTimingResult.Empty;
        }

        var lapse = lapseDuration ?? _defaultLapseDuration;
        if (lapse <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lapseDuration), "Lapse duration must be greater than zero.");
        }

        if (!_frequencyPolicies.TryGetValue(context.Urgency, out var policy))
        {
            throw new InvalidOperationException($"No notification frequency policy configured for urgency '{context.Urgency}'.");
        }

        var elapsed = context.CurrentTime - context.ScheduledAt;
        if (elapsed <= TimeSpan.Zero)
        {
            return NotificationTimingResult.Empty;
        }

        var elapsedSeconds = elapsed.TotalSeconds;
        var lapseSeconds = lapse.TotalSeconds;
        var progress = elapsedSeconds / lapseSeconds;

        var allCrossed = new List<NotificationOpportunity>();
        var eligible = new List<NotificationOpportunity>();

        for (int i = 0; i < policy.Thresholds.Count; i++)
        {
            var threshold = policy.Thresholds[i];
            if (progress >= threshold)
            {
                var thresholdTime = context.ScheduledAt.AddSeconds(lapseSeconds * threshold);
                var opportunity = new NotificationOpportunity(
                    context.TaskId,
                    context.OccurrenceId,
                    i,
                    threshold,
                    thresholdTime);

                allCrossed.Add(opportunity);

                if (!history.HasBeenEmitted(context.OccurrenceId, i))
                {
                    eligible.Add(opportunity);
                }
            }
        }

        return new NotificationTimingResult(eligible, allCrossed);
    }
}
