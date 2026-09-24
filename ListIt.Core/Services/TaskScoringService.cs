using System;
using ListIt.Core.Models;

namespace ListIt.Core.Services;

/// <summary>
/// Deterministic service computing priority scores for tasks based on:
/// 1. Active working state (+1000.0 boost)
/// 2. Base urgency level (Urgency * 100.0)
/// 3. Deadline proximity / elapsed interval percentage (exponential growth toward deadline)
/// 4. Pass count penalty / missed deadlines (PassCount * 75.0)
/// </summary>
public class TaskScoringService : ITaskScoringService
{
    public const double WorkingBoost = 1000.0;
    public const double UrgencyMultiplier = 100.0;
    public const double MaxTimeScore = 50.0;
    public const double PassMultiplier = 75.0;

    public double CalculateScore(ListitTask task, DateTime currentTime, bool isWorking = false)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var currentUtc = currentTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(currentTime, DateTimeKind.Utc)
            : currentTime.ToUniversalTime();

        // 1. Working boost
        var workScore = isWorking ? WorkingBoost : 0.0;

        // 2. Base urgency score
        var urgencyScore = Math.Clamp(task.Urgency, 1, 6) * UrgencyMultiplier;

        // 3. Deadline proximity score
        var interval = task.Interval > TimeSpan.Zero ? task.Interval : TimeSpan.FromDays(1);
        var deadline = task.GetNextDeadlineUtc();
        var windowStart = deadline - interval;

        double timeScore;
        if (currentUtc <= windowStart)
        {
            timeScore = 0.0;
        }
        else
        {
            var elapsedSeconds = (currentUtc - windowStart).TotalSeconds;
            var progress = elapsedSeconds / interval.TotalSeconds;

            if (progress <= 1.0)
            {
                // Quadratic / exponential acceleration as deadline approaches
                timeScore = MaxTimeScore * Math.Pow(progress, 2);
            }
            else
            {
                // Overdue: continues rising linearly past deadline
                timeScore = MaxTimeScore + MaxTimeScore * (progress - 1.0);
            }
        }

        // 4. Pass penalty score
        var passScore = Math.Max(0, task.PassCount) * PassMultiplier;

        return workScore + urgencyScore + timeScore + passScore;
    }
}
