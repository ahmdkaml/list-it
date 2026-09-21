using System;
using System.Collections.Generic;
using ListIt.Core.Models;

namespace ListIt.Core.Scheduling;

/// <summary>
/// Application runtime engine that periodically evaluates scheduling state against current time
/// and provides occurrence lifecycle coordination independent of presentation layers.
/// </summary>
public interface ISchedulingRuntime : IDisposable
{
    bool IsRunning { get; }
    void Start();
    void Stop();

    event EventHandler<IReadOnlyList<EvaluatedOccurrence>>? StateEvaluated;
    IReadOnlyList<EvaluatedOccurrence> CurrentEvaluations { get; }

    IReadOnlyList<EvaluatedOccurrence> EvaluateNow(DateTime? currentTime = null);

    void StartWorking(TaskOccurrence occurrence);
    void StopWorking(TaskOccurrence occurrence);
    void CompleteOccurrence(TaskOccurrence occurrence);
    void MarkOccurrenceMissed(TaskOccurrence occurrence);

    IReadOnlyList<EvaluatedOccurrence> GetOccurrencesForTask(Guid taskId);
    TaskOccurrence? GetOccurrence(Guid occurrenceId);
}
