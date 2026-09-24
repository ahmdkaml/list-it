using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Transforms task scheduling information into concrete TaskOccurrence instances for a requested date or range.
/// Pure deterministic generator: does not read system clock, does not evaluate overdue/missed states,
/// and does not mutate source tasks.
/// </summary>
public class OccurrenceGenerator : IOccurrenceGenerator
{
    public IReadOnlyList<TaskOccurrence> Generate(ListitTask task, DateOnly date)
    {
        return Generate(task, date, date);
    }

    public IReadOnlyList<TaskOccurrence> Generate(ListitTask task, DateOnly startDate, DateOnly endDate)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date cannot be after end date.", nameof(startDate));
        }

        if (task.Type == TaskType.Recurring)
        {
            var occurrences = new List<TaskOccurrence>();
            var windowStart = startDate.ToDateTime(TimeOnly.MinValue);
            var windowEnd = endDate.ToDateTime(TimeOnly.MaxValue);

            if (windowEnd < task.StartTime || task.Interval <= TimeSpan.Zero)
            {
                return Array.Empty<TaskOccurrence>();
            }

            var current = task.StartTime;
            if (current < windowStart)
            {
                var diffTicks = (windowStart - current).Ticks;
                var intervals = diffTicks / task.Interval.Ticks;
                current = current.AddTicks(intervals * task.Interval.Ticks);
                if (current < windowStart)
                {
                    current = current.Add(task.Interval);
                }
            }

            while (current <= windowEnd)
            {
                occurrences.Add(new TaskOccurrence(task.Id, current));
                current = current.Add(task.Interval);
            }

            return occurrences.OrderBy(o => o.ScheduledAt).ToList().AsReadOnly();
        }

        // Finite tasks do not have recurring schedules across dates; their occurrences are tracked per deadline
        return Array.Empty<TaskOccurrence>();
    }
}
