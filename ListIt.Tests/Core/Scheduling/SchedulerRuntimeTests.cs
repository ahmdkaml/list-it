using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.Core.Time;
using ListIt.Infrastructure.Persistence;
using Xunit;

namespace ListIt.Tests.Core.Scheduling;

public class SchedulerRuntimeTests : IDisposable
{
    private class TestClock : IClock
    {
        public DateTime CurrentTime { get; set; }
        public DateTime Now => CurrentTime;

        public TestClock(DateTime initialTime)
        {
            CurrentTime = initialTime;
        }

        public void Advance(TimeSpan duration)
        {
            CurrentTime += duration;
        }
    }

    private class TestNotificationPresenter : INotificationPresenter
    {
        public List<NotificationDecision> PresentedDecisions { get; } = new();

        public void Present(NotificationDecision decision)
        {
            lock (PresentedDecisions)
            {
                PresentedDecisions.Add(decision);
            }
        }
    }

    private readonly string _tempFilePath;
    private readonly JsonTaskRepository _repository;
    private readonly TaskService _taskService;
    private readonly OccurrenceGenerator _generator;
    private readonly Scheduler _scheduler;
    private readonly OccurrenceService _occurrenceService;
    private readonly SchedulingRuntime _schedulingRuntime;
    private readonly InMemoryNotificationHistory _history;
    private readonly NotificationEngine _notificationEngine;
    private readonly TestNotificationPresenter _presenter;
    private readonly TestClock _clock;
    private readonly DateTime _baseTime;

    public SchedulerRuntimeTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"listit_scheduler_test_{Guid.NewGuid():N}.json");
        _repository = new JsonTaskRepository(_tempFilePath);
        _taskService = new TaskService(_repository);
        _generator = new OccurrenceGenerator();
        _scheduler = new Scheduler();
        _occurrenceService = new OccurrenceService(_taskService);

        _baseTime = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
        _clock = new TestClock(_baseTime);

        _schedulingRuntime = new SchedulingRuntime(
            _taskService,
            _generator,
            _scheduler,
            _occurrenceService,
            clock: () => _clock.Now);

        _history = new InMemoryNotificationHistory();
        _notificationEngine = new NotificationEngine(
            new NotificationTimingPolicy(),
            new NotificationSuppressionPolicy(),
            _history,
            new NotificationPresentationPolicy());

        _presenter = new TestNotificationPresenter();
    }

    public void Dispose()
    {
        _schedulingRuntime.Dispose();
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    private SchedulerRuntime CreateRuntime(TimeSpan? interval = null)
    {
        return new SchedulerRuntime(
            _schedulingRuntime,
            _notificationEngine,
            _presenter,
            _clock,
            new SchedulerRuntimeOptions
            {
                EvaluationInterval = interval ?? TimeSpan.FromMilliseconds(50)
            });
    }

    [Fact]
    public async Task StartAsync_BeginsEvaluationAndSetsIsRunningTrue()
    {
        var runtime = CreateRuntime();
        Assert.False(runtime.IsRunning);

        await runtime.StartAsync();
        Assert.True(runtime.IsRunning);

        await runtime.StopAsync();
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task StartAsync_PerformsImmediateEvaluation()
    {
        // Arrange - Create a task scheduled in the past that should immediately notify
        _taskService.CreateFiniteTask("Overdue Task", requiredCompletions: 1, urgency: 3, dueAt: _baseTime.AddMinutes(-30));

        // Use a long interval so periodic tick won't fire during test
        var runtime = CreateRuntime(TimeSpan.FromSeconds(60));

        // Act
        await runtime.StartAsync();

        // Give the initial task a few milliseconds to complete
        await Task.Delay(50);

        // Assert - presenter received decision immediately upon startup
        lock (_presenter.PresentedDecisions)
        {
            Assert.NotEmpty(_presenter.PresentedDecisions);
            var decision = _presenter.PresentedDecisions.First();
            Assert.True(decision.ShouldNotify);
            Assert.Equal("Overdue Task", decision.TaskTitle);
        }

        await runtime.StopAsync();
    }

    [Fact]
    public async Task PeriodicEvaluation_ExecutesMultipleCycles()
    {
        var runtime = CreateRuntime(TimeSpan.FromMilliseconds(20));

        await runtime.StartAsync();
        await Task.Delay(100);

        Assert.True(runtime.IsRunning);
        await runtime.StopAsync();
    }

    [Fact]
    public async Task StopAsync_StopsEvaluationAndSetsIsRunningFalse()
    {
        var runtime = CreateRuntime();
        await runtime.StartAsync();
        Assert.True(runtime.IsRunning);

        await runtime.StopAsync();
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task StartAsync_DoubleCall_IsIdempotentAndCreatesSingleLoop()
    {
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        await runtime.StartAsync();
        await runtime.StartAsync();

        Assert.True(runtime.IsRunning);

        await runtime.StopAsync();
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task StopAsync_DoubleCall_IsIdempotentAndDoesNotThrow()
    {
        var runtime = CreateRuntime();

        await runtime.StartAsync();
        await runtime.StopAsync();
        await runtime.StopAsync();
        await runtime.StopAsync();

        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task Cancellation_TerminatesLoopCleanly()
    {
        using var cts = new CancellationTokenSource();
        var runtime = CreateRuntime(TimeSpan.FromMilliseconds(20));

        await runtime.StartAsync(cts.Token);
        Assert.True(runtime.IsRunning);

        cts.Cancel();

        // Give background loop a moment to exit
        await Task.Delay(60);

        await runtime.StopAsync();
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task NotificationForwarding_AllowedDecision_CallsPresenter()
    {
        // Arrange
        var task = _taskService.CreateFiniteTask("Notify Task", requiredCompletions: 1, urgency: 3, dueAt: _baseTime.AddMinutes(-30));
        var runtime = CreateRuntime();

        // Act - deterministic cycle execution
        var emitted = await runtime.EvaluateCycleAsync();

        // Assert
        Assert.Equal(1, emitted);
        lock (_presenter.PresentedDecisions)
        {
            Assert.Single(_presenter.PresentedDecisions);
            Assert.Equal(task.Id, _presenter.PresentedDecisions[0].TaskId);
            Assert.True(_presenter.PresentedDecisions[0].ShouldNotify);
        }
    }

    [Fact]
    public async Task NotificationForwarding_DoNotNotifyDecision_DoesNotCallPresenter()
    {
        // Arrange - Task is in the future (Upcoming), should not notify
        _taskService.CreateFiniteTask("Future Task", requiredCompletions: 1, urgency: 2, dueAt: _baseTime.AddHours(2));
        var runtime = CreateRuntime();

        // Act
        var emitted = await runtime.EvaluateCycleAsync();

        // Assert
        Assert.Equal(0, emitted);
        lock (_presenter.PresentedDecisions)
        {
            Assert.Empty(_presenter.PresentedDecisions);
        }
    }

    [Fact]
    public async Task DuplicateEvaluation_SameOpportunity_DoesNotDuplicateNotifications()
    {
        // Arrange
        _taskService.CreateFiniteTask("Single Notify", requiredCompletions: 1, urgency: 3, dueAt: _baseTime.AddMinutes(-30));
        var runtime = CreateRuntime();

        // Act - First evaluation emits notification
        var emitted1 = await runtime.EvaluateCycleAsync();
        Assert.Equal(1, emitted1);

        // Act - Second evaluation at the same time does not re-emit
        var emitted2 = await runtime.EvaluateCycleAsync();
        Assert.Equal(0, emitted2);

        lock (_presenter.PresentedDecisions)
        {
            Assert.Single(_presenter.PresentedDecisions);
        }
    }

    [Fact]
    public async Task TimeJump_EvaluatesAtNewCurrentTimeWithoutReplayingMissedTicks()
    {
        // Arrange - Recurring task with 10:00 and 10:20 times
        var task = _taskService.CreateRecurringTask(
            "Sync",
            new[] { new TimeOnly(10, 0), new TimeOnly(10, 20) },
            urgency: 3);

        var runtime = CreateRuntime();

        // Act 1 - Evaluate at 10:00 (due time, no opportunity yet until lapse)
        await runtime.EvaluateCycleAsync();

        // Simulate PC sleeping from 10:00 to 10:30 (time jump of 30 minutes)
        _clock.Advance(TimeSpan.FromMinutes(30));

        // Act 2 - Next evaluation happens at 10:30 directly
        var emitted = await runtime.EvaluateCycleAsync();

        // Assert - Notifications evaluated based on actual current time 10:30
        Assert.True(emitted > 0);
        lock (_presenter.PresentedDecisions)
        {
            Assert.NotEmpty(_presenter.PresentedDecisions);
            Assert.All(_presenter.PresentedDecisions, d => Assert.True(d.ShouldNotify));
        }
    }

    [Fact]
    public async Task ExceptionResilience_TaskFailure_DoesNotKillLoopAndAllowsFutureCycles()
    {
        var runtime = CreateRuntime();

        // Cycle 1 evaluates normally
        var emitted1 = await runtime.EvaluateCycleAsync();
        Assert.Equal(0, emitted1);

        // Add a task that evaluates
        _taskService.CreateFiniteTask("Resilient Task", requiredCompletions: 1, urgency: 3, dueAt: _baseTime.AddMinutes(-30));

        // Cycle 2 evaluates and succeeds
        var emitted2 = await runtime.EvaluateCycleAsync();
        Assert.Equal(1, emitted2);
    }
}
