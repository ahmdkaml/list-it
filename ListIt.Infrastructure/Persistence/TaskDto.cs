using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ListIt.Core.Models;

namespace ListIt.Infrastructure.Persistence;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RecurringTaskDto), "recurring")]
[JsonDerivedType(typeof(FiniteTaskDto), "finite")]
[JsonDerivedType(typeof(UnifiedTaskDto), "task")]
internal abstract class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Urgency { get; set; } = 1;
    public bool BypassPrioritySuppression { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan? Interval { get; set; }
    public int Passes { get; set; }

    [JsonPropertyName("CreatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LegacyCreatedAt
    {
        get => null;
        set
        {
            if (value.HasValue && StartTime == default)
            {
                StartTime = value.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value.Value.ToUniversalTime();
            }
        }
    }

    protected DateTime GetNormalizedStartTime()
    {
        if (StartTime == default)
        {
            return DateTime.UtcNow;
        }

        return StartTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(StartTime, DateTimeKind.Utc)
            : StartTime.ToUniversalTime();
    }

    public abstract ListitTask ToDomain();

    public static TaskDto FromDomain(ListitTask task)
    {
        if (task.Type == TaskType.Recurring)
        {
            return new RecurringTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Urgency = task.Urgency,
                BypassPrioritySuppression = task.BypassPrioritySuppression,
                StartTime = task.StartTime,
                Interval = task.Interval,
                Passes = task.Passes
            };
        }
        else
        {
            return new FiniteTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Urgency = task.Urgency,
                BypassPrioritySuppression = task.BypassPrioritySuppression,
                StartTime = task.StartTime,
                Interval = task.Interval,
                Passes = task.Passes,
                RequiredCompletions = task.RequiredCompletions,
                CurrentCompletions = task.CurrentCompletions
            };
        }
    }
}

internal class RecurringTaskDto : TaskDto
{
    // Legacy support for AssignedTimes
    public List<TimeOnly>? AssignedTimes { get; set; }

    public override ListitTask ToDomain()
    {
        var startTime = GetNormalizedStartTime();
        var interval = Interval ?? TimeSpan.FromDays(1);
        if (interval <= TimeSpan.Zero) interval = TimeSpan.FromDays(1);

        return new ListitTask(
            Id,
            Title,
            TaskType.Recurring,
            interval,
            Description,
            Urgency,
            startTime,
            requiredCompletions: 1,
            currentCompletions: 0,
            passes: Passes,
            bypassPrioritySuppression: BypassPrioritySuppression);
    }
}

internal class FiniteTaskDto : TaskDto
{
    public int RequiredCompletions { get; set; } = 1;
    public int CurrentCompletions { get; set; }

    // Legacy support for DueAt
    public DateTime? DueAt { get; set; }

    public override ListitTask ToDomain()
    {
        var startTime = GetNormalizedStartTime();
        TimeSpan interval;
        if (Interval.HasValue && Interval.Value > TimeSpan.Zero)
        {
            interval = Interval.Value;
        }
        else if (DueAt.HasValue && DueAt.Value > startTime)
        {
            interval = DueAt.Value - startTime;
        }
        else
        {
            interval = TimeSpan.FromDays(1);
        }

        return new ListitTask(
            Id,
            Title,
            TaskType.Finite,
            interval,
            Description,
            Urgency,
            startTime,
            requiredCompletions: RequiredCompletions < 1 ? 1 : RequiredCompletions,
            currentCompletions: CurrentCompletions < 0 ? 0 : CurrentCompletions,
            passes: Passes,
            bypassPrioritySuppression: BypassPrioritySuppression);
    }
}

internal class UnifiedTaskDto : TaskDto
{
    public TaskType TaskType { get; set; } = TaskType.Recurring;
    public int RequiredCompletions { get; set; } = 1;
    public int CurrentCompletions { get; set; }

    public override ListitTask ToDomain()
    {
        var startTime = GetNormalizedStartTime();
        var interval = Interval ?? TimeSpan.FromDays(1);
        if (interval <= TimeSpan.Zero) interval = TimeSpan.FromDays(1);

        return new ListitTask(
            Id,
            Title,
            TaskType,
            interval,
            Description,
            Urgency,
            startTime,
            requiredCompletions: RequiredCompletions < 1 ? 1 : RequiredCompletions,
            currentCompletions: CurrentCompletions < 0 ? 0 : CurrentCompletions,
            passes: Passes,
            bypassPrioritySuppression: BypassPrioritySuppression);
    }
}
