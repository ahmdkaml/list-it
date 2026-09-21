using System;

namespace ListIt.Core.Models;

/// <summary>
/// Abstract base entity containing core properties and invariants shared by all task types.
/// </summary>
public abstract class TaskBase
{
    public Guid Id { get; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public int Urgency { get; private set; }
    public bool BypassPrioritySuppression { get; private set; }
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Identifies the concrete task type. Strictly determined by the derived class.
    /// </summary>
    public abstract TaskType Type { get; }

    protected TaskBase(string title, string description = "", int urgency = 1, bool bypassPrioritySuppression = false)
        : this(Guid.NewGuid(), title, description, urgency, DateTime.UtcNow, bypassPrioritySuppression)
    {
    }

    protected TaskBase(Guid id, string title, string description, int urgency, DateTime createdAt, bool bypassPrioritySuppression = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task ID cannot be empty.", nameof(id));
        }

        if (createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("CreatedAt timestamp must be in UTC.", nameof(createdAt));
        }

        Id = id;
        CreatedAt = createdAt;
        Title = ValidateAndNormalizeTitle(title);
        Description = description ?? string.Empty;
        Urgency = ValidateUrgency(urgency);
        BypassPrioritySuppression = bypassPrioritySuppression;
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
}
