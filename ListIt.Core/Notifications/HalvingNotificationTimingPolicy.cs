using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Models;

namespace ListIt.Core.Notifications;

/// <summary>
/// Progressive halving (Zeno-style) notification timing policy that alerts at 50%, 75%, 87.5%, etc.
/// of elapsed time within a task's interval leading up to the deadline.
/// </summary>
public class HalvingNotificationTimingPolicy : INotificationTimingPolicy
{
    public const int DefaultMaxSteps = 5;
    private readonly IReadOnlyList<double> _thresholds;

    public IReadOnlyList<double> Thresholds => _thresholds;

    public HalvingNotificationTimingPolicy(int maxSteps = DefaultMaxSteps)
        : this(CalculateHalvingThresholds(maxSteps))
    {
    }

    public HalvingNotificationTimingPolicy(IEnumerable<double> thresholds)
    {
        if (thresholds == null) throw new ArgumentNullException(nameof(thresholds));

        var list = thresholds.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("Halving timing policy must define at least one threshold.", nameof(thresholds));
        }

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t <= 0.0 || t >= 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholds), t, "Thresholds must be strictly between 0.0 and 1.0.");
            }

            if (i > 0 && t <= list[i - 1])
            {
                throw new ArgumentException("Thresholds must be strictly increasing.", nameof(thresholds));
            }
        }

        _thresholds = list.AsReadOnly();
    }

    /// <summary>
    /// Generates the mathematical halving thresholds: 1 - (1 / 2^(k+1)) for k from 0 to count - 1.
    /// Alert 0: 0.50 (50%)
    /// Alert 1: 0.75 (75%)
    /// Alert 2: 0.875 (87.5%)
    /// Alert 3: 0.9375 (93.75%)
    /// Alert 4: 0.96875 (96.875%)
    /// </summary>
    public static IReadOnlyList<double> CalculateHalvingThresholds(int count = DefaultMaxSteps)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Step count must be at least 1.");
        }

        var thresholds = new List<double>(count);
        for (int k = 0; k < count; k++)
        {
            thresholds.Add(1.0 - Math.Pow(0.5, k + 1));
        }

        return thresholds.AsReadOnly();
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

        // 2. Determine interval duration and window
        var interval = context.Task.Interval > TimeSpan.Zero
            ? context.Task.Interval
            : (lapseDuration ?? TimeSpan.FromHours(1));

        if (interval <= TimeSpan.Zero)
        {
            return NotificationTimingResult.Empty;
        }

        var deadline = context.ScheduledAt;
        var windowStart = deadline - interval;

        // If evaluated time is before the start of the interval, no alerts
        if (context.CurrentTime < windowStart)
        {
            return NotificationTimingResult.Empty;
        }

        var elapsed = context.CurrentTime - windowStart;
        var progress = elapsed.TotalSeconds / interval.TotalSeconds;

        var allCrossed = new List<NotificationOpportunity>();
        var eligible = new List<NotificationOpportunity>();

        for (int i = 0; i < _thresholds.Count; i++)
        {
            var threshold = _thresholds[i];
            if (progress >= threshold)
            {
                var thresholdTime = windowStart.AddSeconds(interval.TotalSeconds * threshold);
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
