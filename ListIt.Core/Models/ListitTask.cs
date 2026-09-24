using System;

namespace ListIt.Core.Models;

/// <summary>
/// Domain entity representing a task in ListIt.
/// Unifies recurring and finite tasks with StartTime, Interval, and Pass tracking.
/// </summary>
public class ListitTask
{
    public Guid Id { get; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public int Urgency { get; private set; }
    public bool BypassPrioritySuppression { get; private set; }
    public DateTime StartTime { get; private set; }
    public TimeSpan Interval { get; private set; }
    public TaskType Type { get; private set; }
    public int RequiredCompletions { get; private set; }
    public int CurrentCompletions { get; private set; }
    public int Passes { get; private set; }
    public int PassCount => Passes;

    public ListitTask(
        string title,
        TaskType type = TaskType.Recurring,
        TimeSpan? interval = null,
        string description = "",
        int urgency = 1,
        DateTime? startTime = null,
        int requiredCompletions = 1,
        int currentCompletions = 0,
        int passes = 0,
        bool bypassPrioritySuppression = false)
        : this(
            Guid.NewGuid(),
            title,
            type,
            interval ?? TimeSpan.FromDays(1),
            description,
            urgency,
            startTime ?? DateTime.UtcNow,
            requiredCompletions,
            currentCompletions,
            passes,
            bypassPrioritySuppression)
    {
    }

    public ListitTask(
        Guid id,
        string title,
        TaskType type,
        TimeSpan interval,
        string description,
        int urgency,
        DateTime startTime,
        int requiredCompletions = 1,
        int currentCompletions = 0,
        int passes = 0,
        bool bypassPrioritySuppression = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task ID cannot be empty.", nameof(id));
        }

        if (startTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("StartTime timestamp must be in UTC.", nameof(startTime));
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be greater than zero.");
        }

        if (passes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(passes), passes, "Passes cannot be negative.");
        }

        if (type == TaskType.Finite)
        {
            ValidateCompletions(requiredCompletions, currentCompletions);
        }
        else
        {
            if (requiredCompletions < 1) requiredCompletions = 1;
            if (currentCompletions < 0) currentCompletions = 0;
        }

        Id = id;
        Title = ValidateAndNormalizeTitle(title);
        Description = description ?? string.Empty;
        Urgency = ValidateUrgency(urgency);
        BypassPrioritySuppression = bypassPrioritySuppression;
        StartTime = startTime;
        Interval = interval;
        Type = type;
        RequiredCompletions = requiredCompletions;
        CurrentCompletions = currentCompletions;
        Passes = passes;
    }

    public void SetTitle(string title)
    {
        Title = ValidateAndNormalizeTitle(title);
    }

    public void SetDescription(string description)
    {
        Description = description ?? string.Empty;
    }

    public void SetUrgency(int urgency)
    {
        Urgency = ValidateUrgency(urgency);
    }

    public void SetBypassPrioritySuppression(bool bypass)
    {
        BypassPrioritySuppression = bypass;
    }

    public void SetStartTime(DateTime startTime)
    {
        if (startTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("StartTime timestamp must be in UTC.", nameof(startTime));
        }

        StartTime = startTime;
    }

    public void SetInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be greater than zero.");
        }

        Interval = interval;
    }

    public void SetType(TaskType type)
    {
        Type = type;
        if (type == TaskType.Finite && RequiredCompletions < 1)
        {
            RequiredCompletions = 1;
        }
    }

    public void SetRequiredCompletions(int requiredCompletions)
    {
        ValidateCompletions(requiredCompletions, CurrentCompletions);
        RequiredCompletions = requiredCompletions;
    }

    public void RecordCompletion()
    {
        if (Type == TaskType.Finite && CurrentCompletions >= RequiredCompletions)
        {
            throw new InvalidOperationException("Task has already reached the required number of completions.");
        }

        CurrentCompletions++;
    }

    public void ResetCompletions()
    {
        CurrentCompletions = 0;
    }

    public void RecordPass()
    {
        Passes++;
    }

    public void ResetPasses()
    {
        Passes = 0;
    }

    /// <summary>
    /// Resets the interval anchor, starting the countdown anew from the specified time (or UtcNow) and resetting active passes to 0.
    /// </summary>
    public void ResetInterval(DateTime? newStartTime = null)
    {
        var anchor = newStartTime ?? DateTime.UtcNow;
        StartTime = anchor.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(anchor, DateTimeKind.Utc)
            : anchor.ToUniversalTime();
        Passes = 0;
    }

    /// <summary>
    /// Evaluates if unfulfilled interval deadlines have elapsed against the evaluated current time.
    /// If unfulfilled deadlines have elapsed, advances Passes by the number of elapsed intervals and returns true.
    /// </summary>
    public bool CheckAndAdvancePasses(DateTime currentTime)
    {
        if (Interval <= TimeSpan.Zero)
        {
            return false;
        }

        var nextDeadline = GetNextDeadlineUtc();
        if (currentTime < nextDeadline)
        {
            return false;
        }

        var elapsedTicks = (currentTime - StartTime).Ticks;
        var totalIntervalsElapsed = (int)(elapsedTicks / Interval.Ticks);
        if (totalIntervalsElapsed > Passes)
        {
            Passes = totalIntervalsElapsed;
            return true;
        }

        return false;
    }

    public DateTime GetNextDeadlineUtc()
    {
        return StartTime.AddTicks(Interval.Ticks * (Passes + 1));
    }

    public void UpdateDetails(string title, string description)
    {
        SetTitle(title);
        SetDescription(description);
    }

    private static string ValidateAndNormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title cannot be null, empty, or whitespace-only.", nameof(title));
        }

        return title.Trim();
    }

    private static int ValidateUrgency(int urgency)
    {
        if (urgency is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(urgency), urgency, "Urgency must be between 1 and 6.");
        }

        return urgency;
    }

    private static void ValidateCompletions(int required, int current)
    {
        if (required < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(required), required, "Required completions must be at least 1.");
        }

        if (current < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(current), current, "Current completions cannot be negative.");
        }

        if (current > required)
        {
            throw new ArgumentException($"Current completions ({current}) cannot exceed required completions ({required}).");
        }
    }
}
