namespace ListIt.Core.Scheduling;

/// <summary>
/// Represents the temporal scheduling state of a TaskOccurrence relative to an evaluated current time.
/// Pure evaluation state that does not overload or mutate the underlying domain OccurrenceStatus.
/// </summary>
public enum SchedulingState
{
    Upcoming,
    Due,
    Overdue,
    Completed,
    Missed
}
