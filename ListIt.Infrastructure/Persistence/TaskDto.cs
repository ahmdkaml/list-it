using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ListIt.Core.Models;

namespace ListIt.Infrastructure.Persistence;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RecurringTaskDto), "recurring")]
[JsonDerivedType(typeof(FiniteTaskDto), "finite")]
internal abstract class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Urgency { get; set; }
    public bool BypassPrioritySuppression { get; set; }
    public DateTime CreatedAt { get; set; }

    public abstract TaskBase ToDomain();

    public static TaskDto FromDomain(TaskBase task)
    {
        return task switch
        {
            RecurringTask recurring => new RecurringTaskDto
            {
                Id = recurring.Id,
                Title = recurring.Title,
                Description = recurring.Description,
                Urgency = recurring.Urgency,
                BypassPrioritySuppression = recurring.BypassPrioritySuppression,
                CreatedAt = recurring.CreatedAt,
                AssignedTimes = new List<TimeOnly>(recurring.AssignedTimes)
            },
            FiniteTask finite => new FiniteTaskDto
            {
                Id = finite.Id,
                Title = finite.Title,
                Description = finite.Description,
                Urgency = finite.Urgency,
                BypassPrioritySuppression = finite.BypassPrioritySuppression,
                CreatedAt = finite.CreatedAt,
                RequiredCompletions = finite.RequiredCompletions,
                CurrentCompletions = finite.CurrentCompletions,
                DueAt = finite.DueAt
            },
            _ => throw new NotSupportedException($"Unsupported task type: {task.GetType().Name}")
        };
    }
}

internal class RecurringTaskDto : TaskDto
{
    public List<TimeOnly> AssignedTimes { get; set; } = new();

    public override TaskBase ToDomain()
    {
        return new RecurringTask(Id, Title, Description, Urgency, CreatedAt, AssignedTimes, BypassPrioritySuppression);
    }
}

internal class FiniteTaskDto : TaskDto
{
    public int RequiredCompletions { get; set; }
    public int CurrentCompletions { get; set; }
    public DateTime DueAt { get; set; }

    public override TaskBase ToDomain()
    {
        DateTime? dueAt = DueAt == default ? null : DueAt;
        return new FiniteTask(Id, Title, Description, Urgency, CreatedAt, RequiredCompletions, CurrentCompletions, dueAt, BypassPrioritySuppression);
    }
}
