using System;

namespace ListIt.Core.Models;

/// <summary>
/// A task that must be completed a specified number of times.
/// </summary>
public class FiniteTask : TaskBase
{
    public override TaskType Type => TaskType.Finite;

    public int RequiredCompletions { get; private set; }
    public int CurrentCompletions { get; private set; }

    public FiniteTask(
        string title,
        int requiredCompletions,
        int currentCompletions = 0,
        string description = "",
        int urgency = 1)
        : base(title, description, urgency)
    {
        ValidateCompletions(requiredCompletions, currentCompletions);
        RequiredCompletions = requiredCompletions;
        CurrentCompletions = currentCompletions;
    }

    public FiniteTask(
        Guid id,
        string title,
        string description,
        int urgency,
        DateTime createdAt,
        int requiredCompletions,
        int currentCompletions)
        : base(id, title, description, urgency, createdAt)
    {
        ValidateCompletions(requiredCompletions, currentCompletions);
        RequiredCompletions = requiredCompletions;
        CurrentCompletions = currentCompletions;
    }

    public void SetRequiredCompletions(int requiredCompletions)
    {
        ValidateCompletions(requiredCompletions, CurrentCompletions);
        RequiredCompletions = requiredCompletions;
    }

    public void RecordCompletion()
    {
        if (CurrentCompletions >= RequiredCompletions)
        {
            throw new InvalidOperationException("Task has already reached the required number of completions.");
        }

        CurrentCompletions++;
    }

    public void ResetCompletions()
    {
        CurrentCompletions = 0;
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
