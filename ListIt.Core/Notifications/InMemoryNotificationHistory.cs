using System;
using System.Collections.Generic;

namespace ListIt.Core.Notifications;

/// <summary>
/// Thread-safe in-memory implementation of INotificationHistory.
/// </summary>
public class InMemoryNotificationHistory : INotificationHistory
{
    private readonly object _lock = new();
    private readonly HashSet<string> _emittedKeys = new();

    public bool HasBeenEmitted(Guid occurrenceId, int opportunityIndex)
    {
        lock (_lock)
        {
            return _emittedKeys.Contains(GetKey(occurrenceId, opportunityIndex));
        }
    }

    public void RecordEmitted(Guid occurrenceId, int opportunityIndex)
    {
        lock (_lock)
        {
            _emittedKeys.Add(GetKey(occurrenceId, opportunityIndex));
        }
    }

    public void Clear(Guid occurrenceId)
    {
        lock (_lock)
        {
            var prefix = $"{occurrenceId:D}_";
            _emittedKeys.RemoveWhere(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string GetKey(Guid occurrenceId, int opportunityIndex) =>
        $"{occurrenceId:D}_{opportunityIndex}";
}
