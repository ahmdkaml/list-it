using System;
using System.Collections.Generic;
using System.Linq;

namespace ListIt.Core.Notifications;

/// <summary>
/// Defines the normalized notification opportunity thresholds for a given urgency level within a lapse.
/// Thresholds are represented as fractions of a full lapse (e.g. 0.50 = 50%, 1.00 = 100%).
/// </summary>
public class NotificationFrequencyPolicy
{
    public int Urgency { get; }
    public IReadOnlyList<double> Thresholds { get; }

    public NotificationFrequencyPolicy(int urgency, IEnumerable<double> thresholds)
    {
        if (urgency is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(urgency), urgency, "Urgency must be between 1 and 6.");
        }

        if (thresholds == null)
        {
            throw new ArgumentNullException(nameof(thresholds));
        }

        var list = thresholds.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("Frequency policy must define at least one threshold.", nameof(thresholds));
        }

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t <= 0.0 || t > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholds), t, "Thresholds must be greater than 0.0 and less than or equal to 1.0.");
            }

            if (i > 0 && t <= list[i - 1])
            {
                throw new ArgumentException("Thresholds must be strictly increasing.", nameof(thresholds));
            }
        }

        Urgency = urgency;
        Thresholds = list.AsReadOnly();
    }

    /// <summary>
    /// Returns default progressive frequency policies for urgencies 1 through 6.
    /// </summary>
    public static IReadOnlyDictionary<int, NotificationFrequencyPolicy> GetDefaultPolicies()
    {
        return new Dictionary<int, NotificationFrequencyPolicy>
        {
            [1] = new(1, new[] { 1.0 }),
            [2] = new(2, new[] { 0.5, 1.0 }),
            [3] = new(3, new[] { 0.33, 0.67, 1.0 }),
            [4] = new(4, new[] { 0.25, 0.5, 0.75, 1.0 }),
            [5] = new(5, new[] { 0.20, 0.40, 0.60, 0.80, 1.0 }),
            [6] = new(6, new[] { 0.15, 0.30, 0.45, 0.60, 0.75, 0.90, 1.0 })
        };
    }
}
