using System;
using ListIt.Core.Models;

namespace ListIt.Core.Services;

/// <summary>
/// Contract for calculating dynamic priority scores for tasks based on urgency,
/// time remaining to deadline, missed passes, and active working state.
/// </summary>
public interface ITaskScoringService
{
    /// <summary>
    /// Computes a priority score for the specified task. Higher score indicates higher priority.
    /// </summary>
    double CalculateScore(ListitTask task, DateTime currentTime, bool isWorking = false);
}
