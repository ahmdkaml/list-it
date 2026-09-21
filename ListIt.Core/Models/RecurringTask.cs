using System;
using System.Collections.Generic;
using System.Linq;

namespace ListIt.Core.Models;

/// <summary>
/// A task that remains active indefinitely and has one or more assigned daily times.
/// </summary>
public class RecurringTask : TaskBase
{
    public override TaskType Type => TaskType.Recurring;

    public IReadOnlyList<TimeOnly> AssignedTimes { get; private set; }

    public RecurringTask(
        string title,
        IEnumerable<TimeOnly> assignedTimes,
        string description = "",
        int urgency = 1,
        bool bypassPrioritySuppression = false)
        : base(title, description, urgency, bypassPrioritySuppression)
    {
        AssignedTimes = ValidateAndNormalizeAssignedTimes(assignedTimes);
    }

    public RecurringTask(
        Guid id,
        string title,
        string description,
        int urgency,
        DateTime createdAt,
        IEnumerable<TimeOnly> assignedTimes,
        bool bypassPrioritySuppression = false)
        : base(id, title, description, urgency, createdAt, bypassPrioritySuppression)
    {
        AssignedTimes = ValidateAndNormalizeAssignedTimes(assignedTimes);
    }

    public void SetAssignedTimes(IEnumerable<TimeOnly> assignedTimes)
    {
        AssignedTimes = ValidateAndNormalizeAssignedTimes(assignedTimes);
    }

    private static IReadOnlyList<TimeOnly> ValidateAndNormalizeAssignedTimes(IEnumerable<TimeOnly> assignedTimes)
    {
        if (assignedTimes == null)
        {
            throw new ArgumentNullException(nameof(assignedTimes), "Assigned times collection cannot be null.");
        }

        var normalized = assignedTimes.Distinct().OrderBy(t => t).ToList();

        if (normalized.Count == 0)
        {
            throw new ArgumentException("A recurring task must have at least one assigned time.", nameof(assignedTimes));
        }

        return normalized.AsReadOnly();
    }
}
