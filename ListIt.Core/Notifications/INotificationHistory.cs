using System;

namespace ListIt.Core.Notifications;

/// <summary>
/// Abstraction for tracking emitted notification opportunities.
/// Keeps notification history completely separate from occurrence lifecycle state.
/// </summary>
public interface INotificationHistory
{
    bool HasBeenEmitted(Guid occurrenceId, int opportunityIndex);
    void RecordEmitted(Guid occurrenceId, int opportunityIndex);
    void Clear(Guid occurrenceId);
}
