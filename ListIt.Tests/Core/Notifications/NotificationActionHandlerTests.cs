using System;
using System.IO;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.Infrastructure.Persistence;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationActionHandlerTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly JsonTaskRepository _repository;
    private readonly TaskService _taskService;
    private readonly OccurrenceGenerator _generator;
    private readonly Scheduler _scheduler;
    private readonly OccurrenceService _occurrenceService;
    private readonly SchedulingRuntime _runtime;
    private readonly InMemoryNotificationHistory _history;
    private readonly NotificationActionHandler _handler;
    private readonly DateTime _baseTime;

    public NotificationActionHandlerTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"listit_action_test_{Guid.NewGuid():N}.json");
        _repository = new JsonTaskRepository(_tempFilePath);
        _taskService = new TaskService(_repository);
        _generator = new OccurrenceGenerator();
        _scheduler = new Scheduler();
        _occurrenceService = new OccurrenceService(_taskService);
        _baseTime = new DateTime(2026, 9, 21, 14, 0, 0);

        _runtime = new SchedulingRuntime(
            _taskService,
            _generator,
            _scheduler,
            _occurrenceService,
            clock: () => _baseTime);

        _history = new InMemoryNotificationHistory();

        _handler = new NotificationActionHandler(
            _runtime,
            _taskService,
            _history);
    }

    public void Dispose()
    {
        _runtime.Dispose();
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void WorkAction_TransitionsOccurrenceToWorking_AndPersists()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Report", requiredCompletions: 1, urgency: 2, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        Assert.False(occ.IsWorking);
        Assert.Equal(OccurrenceStatus.Pending, occ.Status);

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Work);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.True(result.StateChanged);
        Assert.True(occ.IsWorking);
    }

    [Fact]
    public void WorkAction_DoubleClicks_AreIdempotent_AndDoNotThrow()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Report", requiredCompletions: 1, urgency: 2, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Work);

        // First click
        var result1 = _handler.Handle(context);
        Assert.True(result1.IsSuccess);
        Assert.True(result1.StateChanged);

        // Act - Second click (double-click simulation)
        var result2 = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result2.Status);
        Assert.True(result2.ShouldClosePopup);
        Assert.False(result2.StateChanged);
        Assert.True(occ.IsWorking);
    }

    [Fact]
    public void DoneAction_FiniteTask_CompletesOccurrence_IncrementsCount_AndPersists()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Workout", requiredCompletions: 2, urgency: 2, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Done);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.True(result.StateChanged);
        Assert.Equal(OccurrenceStatus.Completed, occ.Status);

        var updatedTask = (FiniteTask)_taskService.GetTask(task.Id)!;
        Assert.Equal(1, updatedTask.CurrentCompletions);
    }

    [Fact]
    public void DoneAction_FiniteTask_DeletesTaskWhenRequiredCountReached()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Quick errand", requiredCompletions: 1, urgency: 2, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Done);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.True(result.StateChanged);
        Assert.Equal(OccurrenceStatus.Completed, occ.Status);

        // Task must be deleted from repository
        Assert.Null(_taskService.GetTask(task.Id));
    }

    [Fact]
    public void DoneAction_RecurringTask_CompletesOccurrence_AndPreservesSchedule()
    {
        // Arrange
        var times = new[] { new TimeOnly(14, 0), new TimeOnly(18, 0) };
        var task = _taskService.CreateRecurringTask("Daily Routine", times, urgency: 2);
        var evaluations = _runtime.EvaluateNow();
        var occ14 = evaluations.First(e => e.Occurrence.ScheduledAt.Hour == 14).Occurrence;

        var context = new NotificationActionContext(task.Id, occ14.OccurrenceId, NotificationAction.Done);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.True(result.StateChanged);
        Assert.Equal(OccurrenceStatus.Completed, occ14.Status);

        // Recurring task still exists with schedule intact
        var preservedTask = (RecurringTask)_taskService.GetTask(task.Id)!;
        Assert.NotNull(preservedTask);
        Assert.Equal(2, preservedTask.AssignedTimes.Count);
    }

    [Fact]
    public void DoneAction_DoubleClicks_AreIdempotent_AndDoNotDoubleCount()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Double Check", requiredCompletions: 3, urgency: 2, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Done);

        // First click
        var result1 = _handler.Handle(context);
        Assert.True(result1.IsSuccess);
        Assert.True(result1.StateChanged);

        // Act - Second click on same completed occurrence
        var result2 = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result2.Status);
        Assert.True(result2.ShouldClosePopup);
        Assert.False(result2.StateChanged);

        var taskAfter = (FiniteTask)_taskService.GetTask(task.Id)!;
        Assert.Equal(1, taskAfter.CurrentCompletions); // Still 1, NOT 2
    }

    [Fact]
    public void DismissAction_ClosesPopup_LeavesOccurrencePending_AndDoesNotIncrementCount()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Optional Read", requiredCompletions: 2, urgency: 1, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Dismiss);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.Success, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.False(result.StateChanged);

        // Occurrence remains pending
        Assert.Equal(OccurrenceStatus.Pending, occ.Status);
        Assert.False(occ.IsWorking);

        // Count unchanged
        var taskAfter = (FiniteTask)_taskService.GetTask(task.Id)!;
        Assert.Equal(0, taskAfter.CurrentCompletions);
    }

    [Fact]
    public void DismissAction_WithOpportunityIndex_RecordsEmittedInHistory()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Reminder", requiredCompletions: 1, urgency: 1, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        Assert.False(_history.HasBeenEmitted(occ.OccurrenceId, 3));

        var context = new NotificationActionContext(
            task.Id,
            occ.OccurrenceId,
            NotificationAction.Dismiss,
            opportunityIndex: 3);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(_history.HasBeenEmitted(occ.OccurrenceId, 3));
    }

    [Fact]
    public void StaleNotification_CompletedOccurrence_FailsSafelyAndClosesPopup()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Stale", requiredCompletions: 1, urgency: 1, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;
        occ.MarkMissed(); // Manually set to Missed (stale)

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Done);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.InvalidState, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.False(result.StateChanged);
    }

    [Fact]
    public void DeletedTask_FailsSafelyAndClosesPopup_WithoutException()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("To Delete", requiredCompletions: 1, urgency: 1, dueAt: _baseTime.AddMinutes(10));
        var evaluations = _runtime.EvaluateNow();
        var occ = evaluations.Single(e => e.Task.Id == task.Id).Occurrence;

        // Delete task from service
        _taskService.DeleteTask(task.Id);

        var context = new NotificationActionContext(task.Id, occ.OccurrenceId, NotificationAction.Done);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.NotFound, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.False(result.StateChanged);
    }

    [Fact]
    public void UnknownOccurrence_FailsSafelyAndClosesPopup()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Existing Task", requiredCompletions: 1, urgency: 1, dueAt: _baseTime.AddMinutes(10));
        var context = new NotificationActionContext(task.Id, Guid.NewGuid(), NotificationAction.Work);

        // Act
        var result = _handler.Handle(context);

        // Assert
        Assert.Equal(NotificationActionStatus.NotFound, result.Status);
        Assert.True(result.ShouldClosePopup);
        Assert.False(result.StateChanged);
    }
}
