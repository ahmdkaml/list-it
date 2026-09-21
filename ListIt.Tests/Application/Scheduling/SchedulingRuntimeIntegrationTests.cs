using System;
using System.IO;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.Infrastructure.Persistence;
using Xunit;

namespace ListIt.Tests.Application.Scheduling;

public class SchedulingRuntimeIntegrationTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly JsonTaskRepository _repository;
    private readonly TaskService _taskService;
    private readonly OccurrenceGenerator _generator;
    private readonly Scheduler _scheduler;
    private readonly OccurrenceService _occurrenceService;

    public SchedulingRuntimeIntegrationTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"listit_runtime_test_{Guid.NewGuid():N}.json");
        _repository = new JsonTaskRepository(_tempFilePath);
        _taskService = new TaskService(_repository);
        _generator = new OccurrenceGenerator();
        _scheduler = new Scheduler();
        _occurrenceService = new OccurrenceService(_taskService);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void RecurringTask_Pipeline_EvaluatesAndPreservesScheduleAfterCompletion()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 14, 0, 0);
        using var runtime = new SchedulingRuntime(
            _taskService, _generator, _scheduler, _occurrenceService,
            clock: () => testTime);

        var task = _taskService.CreateRecurringTask(
            "Daily Sync",
            new[] { new TimeOnly(14, 0), new TimeOnly(18, 0) },
            urgency: 2);

        // Act - 1. Initial evaluation
        var evaluations = runtime.EvaluateNow();

        // Assert - 2 occurrences generated and evaluated
        Assert.Equal(2, evaluations.Count);
        var occ14 = evaluations.First(e => e.Occurrence.ScheduledAt.Hour == 14);
        var occ18 = evaluations.First(e => e.Occurrence.ScheduledAt.Hour == 18);

        Assert.Equal(SchedulingState.Due, occ14.State);
        Assert.Equal(SchedulingState.Upcoming, occ18.State);

        // Act - 2. Complete 14:00 occurrence
        runtime.CompleteOccurrence(occ14.Occurrence);

        // Assert - Re-evaluated after completion
        var updatedEvals = runtime.CurrentEvaluations;
        var updatedOcc14 = updatedEvals.First(e => e.Occurrence.ScheduledAt.Hour == 14);
        Assert.Equal(OccurrenceStatus.Completed, updatedOcc14.Occurrence.Status);
        Assert.Equal(SchedulingState.Completed, updatedOcc14.State);

        // Verify task itself remains recurring and schedule is untouched
        var retrievedTask = _taskService.GetTask(task.Id) as RecurringTask;
        Assert.NotNull(retrievedTask);
        Assert.Equal(2, retrievedTask.AssignedTimes.Count);
        Assert.Contains(new TimeOnly(14, 0), retrievedTask.AssignedTimes);
        Assert.Contains(new TimeOnly(18, 0), retrievedTask.AssignedTimes);
    }

    [Fact]
    public void FiniteTask_Pipeline_CompletesIncrementsAndDeletesAtRequiredCount()
    {
        // Arrange
        var dueTime = new DateTime(2026, 9, 21, 16, 0, 0);
        using var runtime = new SchedulingRuntime(
            _taskService, _generator, _scheduler, _occurrenceService,
            clock: () => dueTime);

        var finiteTask = _taskService.CreateFiniteTask(
            "Submit Invoices",
            requiredCompletions: 2,
            dueAt: dueTime);

        // Act & Assert - Completion 1
        var evals1 = runtime.EvaluateNow();
        var eval1 = Assert.Single(evals1);
        Assert.Equal(finiteTask.Id, eval1.Task.Id);
        Assert.Equal(OccurrenceStatus.Pending, eval1.Occurrence.Status);

        runtime.CompleteOccurrence(eval1.Occurrence);

        var taskAfterFirst = _taskService.GetTask(finiteTask.Id) as FiniteTask;
        Assert.NotNull(taskAfterFirst);
        Assert.Equal(1, taskAfterFirst.CurrentCompletions);

        // Act & Assert - Completion 2 (reaches required completions)
        var evals2 = runtime.CurrentEvaluations;
        var eval2 = Assert.Single(evals2);
        Assert.Equal(OccurrenceStatus.Pending, eval2.Occurrence.Status);

        runtime.CompleteOccurrence(eval2.Occurrence);

        // Verify task was deleted according to finite-task completion policy
        var taskAfterSecond = _taskService.GetTask(finiteTask.Id);
        Assert.Null(taskAfterSecond);

        // Evaluations should now be empty
        Assert.Empty(runtime.CurrentEvaluations);
    }

    [Fact]
    public void WorkingState_Pipeline_StartsWorkingAndEndsUponCompletion()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 9, 0, 0);
        using var runtime = new SchedulingRuntime(
            _taskService, _generator, _scheduler, _occurrenceService,
            clock: () => testTime);

        var task = _taskService.CreateRecurringTask("Deep Work", new[] { new TimeOnly(9, 0) });
        var evals = runtime.EvaluateNow();
        var evaluated = Assert.Single(evals);

        Assert.False(evaluated.Occurrence.IsWorking);

        // Act - Start working
        runtime.StartWorking(evaluated.Occurrence);

        // Assert - Occurrence is actively working
        var workingEvals = runtime.CurrentEvaluations;
        var working = Assert.Single(workingEvals);
        Assert.True(working.Occurrence.IsWorking);

        // Act - Complete occurrence
        runtime.CompleteOccurrence(working.Occurrence);

        // Assert - Working state ends
        var completedEvals = runtime.CurrentEvaluations;
        var completed = Assert.Single(completedEvals);
        Assert.False(completed.Occurrence.IsWorking);
        Assert.Equal(OccurrenceStatus.Completed, completed.Occurrence.Status);
        Assert.Equal(SchedulingState.Completed, completed.State);
    }

    [Fact]
    public void SuppressionBypass_ConfigurationPreservedAcrossEvaluations()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 10, 0, 0);
        using var runtime = new SchedulingRuntime(
            _taskService, _generator, _scheduler, _occurrenceService,
            clock: () => testTime);

        var task = _taskService.CreateRecurringTask(
            "Executive Alert",
            new[] { new TimeOnly(10, 0) },
            urgency: 6,
            bypassPrioritySuppression: true);

        // Act
        var evals = runtime.EvaluateNow();

        // Assert
        var evaluated = Assert.Single(evals);
        Assert.True(evaluated.Task.BypassPrioritySuppression);
    }

    [Fact]
    public void DeduplicationAndStateRetention_AcrossRegenerationPasses()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 8, 0, 0);
        using var runtime = new SchedulingRuntime(
            _taskService, _generator, _scheduler, _occurrenceService,
            clock: () => testTime);

        var task = _taskService.CreateRecurringTask("Daily Check", new[] { new TimeOnly(8, 0) });

        // Initial evaluation
        var evals1 = runtime.EvaluateNow();
        var occ1 = evals1[0].Occurrence;

        // Start working on the occurrence
        runtime.StartWorking(occ1);

        // Second evaluation at a later time
        var evals2 = runtime.EvaluateNow(new DateTime(2026, 9, 21, 8, 30, 0));
        var occ2 = evals2[0].Occurrence;

        // Must retain the exact same occurrence instance and working state
        Assert.Same(occ1, occ2);
        Assert.True(occ2.IsWorking);
    }

    [Fact]
    public void Runtime_StartAndStop_ManagesIsRunningState()
    {
        // Arrange
        using var runtime = new SchedulingRuntime(
            _taskService, _generator, _scheduler, _occurrenceService,
            pollInterval: TimeSpan.FromMilliseconds(50));

        Assert.False(runtime.IsRunning);

        // Act
        runtime.Start();
        Assert.True(runtime.IsRunning);

        runtime.Stop();
        Assert.False(runtime.IsRunning);
    }
}
