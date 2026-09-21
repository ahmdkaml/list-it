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
    public IReadOnlyList<TaskOccurrence> Generate(TaskBase task, DateOnly date)
    {
        return Generate(task, date, date);
    }

    public IReadOnlyList<TaskOccurrence> Generate(TaskBase task, DateOnly startDate, DateOnly endDate)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date cannot be after end date.", nameof(startDate));
        }

        if (task is RecurringTask recurringTask)
        {
            var occurrences = new List<TaskOccurrence>();

            for (var currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                foreach (var time in recurringTask.AssignedTimes)
                {
                    var scheduledAt = new DateTime(
                        currentDate.Year,
                        currentDate.Month,
                        currentDate.Day,
                        time.Hour,
                        time.Minute,
                        time.Second,
                        DateTimeKind.Unspecified);

                    occurrences.Add(new TaskOccurrence(recurringTask.Id, scheduledAt));
                }
            }

            // Order deterministically by scheduled date and time ascending
            return occurrences.OrderBy(o => o.ScheduledAt).ToList().AsReadOnly();
        }

        // Finite tasks do not have an automatic recurring schedule in Phase 2.2 (deferred per specification)
        return Array.Empty<TaskOccurrence>();
    }
}
