using System;
using System.Collections.Generic;
using System.Linq;

namespace ListIt.Core.Notifications;

/// <summary>
/// Result of evaluating an occurrence against the notification timing policy.
/// </summary>
public class NotificationTimingResult
{
    public bool ShouldNotify => EligibleOpportunities.Count > 0;
    public IReadOnlyList<NotificationOpportunity> EligibleOpportunities { get; }
    public IReadOnlyList<NotificationOpportunity> AllCrossedOpportunities { get; }

    public NotificationTimingResult(
        IEnumerable<NotificationOpportunity>? eligibleOpportunities = null,
        IEnumerable<NotificationOpportunity>? allCrossedOpportunities = null)
    {
        EligibleOpportunities = (eligibleOpportunities?.ToList() ?? new List<NotificationOpportunity>()).AsReadOnly();
        AllCrossedOpportunities = (allCrossedOpportunities?.ToList() ?? new List<NotificationOpportunity>()).AsReadOnly();
    }

    public static NotificationTimingResult Empty { get; } = new();
}
