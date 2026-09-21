using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Services;
using ListIt.UI.ViewModels;
using Xunit;

namespace ListIt.Tests.UI;

public class MainViewModelTests
{
    private class FakeTaskService : ITaskService
    {
        private readonly List<TaskBase> _tasks = new();

        public int DeleteCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int CreateRecurringCallCount { get; private set; }
        public int CreateFiniteCallCount { get; private set; }

        public FakeTaskService(IEnumerable<TaskBase>? initialTasks = null)
        {
            if (initialTasks != null)
            {
                _tasks.AddRange(initialTasks);
            }
        }

        public IReadOnlyList<TaskBase> GetAllTasks() => _tasks.ToList().AsReadOnly();

        public TaskBase? GetTask(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

        public RecurringTask CreateRecurringTask(string title, IEnumerable<TimeOnly> assignedTimes, string description = "", int urgency = 1, bool bypassPrioritySuppression = false)
        {
            CreateRecurringCallCount++;
            var task = new RecurringTask(title, assignedTimes, description, urgency, bypassPrioritySuppression);
            _tasks.Add(task);
            return task;
        }

        public FiniteTask CreateFiniteTask(string title, int requiredCompletions, string description = "", int urgency = 1, DateTime? dueAt = null, bool bypassPrioritySuppression = false)
        {
            CreateFiniteCallCount++;
            var task = new FiniteTask(title, requiredCompletions, 0, description, urgency, dueAt, bypassPrioritySuppression);
            _tasks.Add(task);
            return task;
        }

        public void UpdateTask(TaskBase task)
        {
            UpdateCallCount++;
            var idx = _tasks.FindIndex(t => t.Id == task.Id);
            if (idx >= 0) _tasks[idx] = task;
        }

        public bool DeleteTask(Guid id)
        {
            DeleteCallCount++;
            var removed = _tasks.RemoveAll(t => t.Id == id) > 0;
            return removed;
        }
    }

    [Fact]
    public void Constructor_LoadsTasksFromService()
    {
        // Arrange
        var initial = new TaskBase[]
        {
            new RecurringTask("Task 1", new[] { new TimeOnly(9, 0) }),
            new FiniteTask("Task 2", 2)
        };
        var service = new FakeTaskService(initial);

        // Act
        var vm = new MainViewModel(service);

        // Assert
        Assert.Equal(2, vm.Tasks.Count);
        Assert.Equal("Task 1", vm.Tasks[0].Title);
        Assert.Equal("Task 2", vm.Tasks[1].Title);
    }

    [Fact]
    public void OpenCreateTaskCommand_OpensEditorInCreateMode()
    {
        // Arrange
        var service = new FakeTaskService();
        var vm = new MainViewModel(service);

        // Act
        vm.OpenCreateTaskCommand.Execute(null);

        // Assert
        Assert.True(vm.IsEditorOpen);
        Assert.False(vm.Editor.IsEditing);
        Assert.True(vm.Editor.IsTypeSelectionEnabled);
    }

    [Fact]
    public void OpenEditTaskCommand_OpensEditorWithSelectedTask()
    {
        // Arrange
        var task = new FiniteTask("Target Task", 3, 1, "Desc", 4);
        var service = new FakeTaskService(new[] { task });
        var vm = new MainViewModel(service);
        vm.SelectedTask = vm.Tasks[0];

        // Act
        vm.OpenEditTaskCommand.Execute(null);

        // Assert
        Assert.True(vm.IsEditorOpen);
        Assert.True(vm.Editor.IsEditing);
        Assert.False(vm.Editor.IsTypeSelectionEnabled);
        Assert.Equal("Target Task", vm.Editor.Title);
        Assert.Equal(3, vm.Editor.RequiredCompletions);
    }

    [Fact]
    public void DeleteTaskCommand_UserConfirms_DeletesTaskAndRefreshes()
    {
        // Arrange
        var task = new FiniteTask("Task to Delete", 1);
        var service = new FakeTaskService(new[] { task });
        var vm = new MainViewModel(service)
        {
            ConfirmDeleteHandler = (msg, title) => true
        };
        vm.SelectedTask = vm.Tasks[0];

        // Act
        vm.DeleteTaskCommand.Execute(null);

        // Assert
        Assert.Equal(1, service.DeleteCallCount);
        Assert.Empty(vm.Tasks);
        Assert.Null(vm.SelectedTask);
    }

    [Fact]
    public void DeleteTaskCommand_UserCancels_DoesNotDeleteTask()
    {
        // Arrange
        var task = new FiniteTask("Task to Keep", 1);
        var service = new FakeTaskService(new[] { task });
        var vm = new MainViewModel(service)
        {
            ConfirmDeleteHandler = (msg, title) => false
        };
        vm.SelectedTask = vm.Tasks[0];

        // Act
        vm.DeleteTaskCommand.Execute(null);

        // Assert
        Assert.Equal(0, service.DeleteCallCount);
        Assert.Single(vm.Tasks);
        Assert.NotNull(vm.SelectedTask);
    }

    [Fact]
    public void RefreshTasksCommand_ReloadsCollection()
    {
        // Arrange
        var service = new FakeTaskService();
        var vm = new MainViewModel(service);
        Assert.Empty(vm.Tasks);

        service.CreateFiniteTask("New Task", 1);

        // Act
        vm.RefreshTasksCommand.Execute(null);

        // Assert
        Assert.Single(vm.Tasks);
        Assert.Equal("New Task", vm.Tasks[0].Title);
    }

    [Fact]
    public void WorkTaskCommand_StartsAndStopsWorking_WhenOccurrencePending()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var service = new FakeTaskService();
        service.CreateRecurringTask("Morning Review", new[] { new TimeOnly(9, 0) });

        var generator = new ListIt.Core.Scheduling.OccurrenceGenerator();
        var scheduler = new ListIt.Core.Scheduling.Scheduler();
        var occurrenceService = new ListIt.Core.Scheduling.OccurrenceService(service);
        using var runtime = new ListIt.Core.Scheduling.SchedulingRuntime(
            service, generator, scheduler, occurrenceService, clock: () => testTime);

        var vm = new MainViewModel(service, runtime);
        runtime.EvaluateNow(testTime);
        vm.SyncEvaluatedStates();

        var item = vm.Tasks[0];
        vm.SelectedTask = item;

        Assert.False(item.IsWorking);
        Assert.Equal("Work", vm.WorkButtonText);
        Assert.True(vm.CanWorkSelectedTask);

        // Act 1: Start Work
        vm.WorkTaskCommand.Execute(null);

        // Assert 1
        Assert.True(item.IsWorking);
        Assert.Equal("Working", item.StateDisplay);
        Assert.Equal("Stop", vm.WorkButtonText);
        Assert.Equal("Stop", item.WorkActionText);

        // Act 2: Stop Work
        vm.WorkTaskCommand.Execute(null);

        // Assert 2
        Assert.False(item.IsWorking);
        Assert.Equal("Work", vm.WorkButtonText);
        Assert.Equal("Work", item.WorkActionText);
        Assert.Equal("Due", item.StateDisplay);
    }

    [Fact]
    public void DoneTaskCommand_RecurringTask_CompletesOccurrence()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var service = new FakeTaskService();
        service.CreateRecurringTask("Daily Sync", new[] { new TimeOnly(9, 0) });

        var generator = new ListIt.Core.Scheduling.OccurrenceGenerator();
        var scheduler = new ListIt.Core.Scheduling.Scheduler();
        var occurrenceService = new ListIt.Core.Scheduling.OccurrenceService(service);
        using var runtime = new ListIt.Core.Scheduling.SchedulingRuntime(
            service, generator, scheduler, occurrenceService, clock: () => testTime);

        var vm = new MainViewModel(service, runtime);
        runtime.EvaluateNow(testTime);
        vm.SyncEvaluatedStates();

        vm.SelectedTask = vm.Tasks[0];
        Assert.True(vm.CanCompleteSelectedTask);

        // Act
        vm.DoneTaskCommand.Execute(null);

        // Assert
        Assert.Single(vm.Tasks);
        Assert.False(vm.Tasks[0].IsWorking);
        Assert.Equal(ListIt.Core.Scheduling.SchedulingState.Completed, vm.Tasks[0].SchedulingState);
        Assert.Equal("Completed", vm.Tasks[0].StateDisplay);
        Assert.False(vm.CanCompleteSelectedTask);
    }

    [Fact]
    public void DoneTaskCommand_FiniteTask_DeletesTaskWhenAllCompletionsMet()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var service = new FakeTaskService();
        service.CreateFiniteTask("Finish Report", requiredCompletions: 1, dueAt: testTime);

        var generator = new ListIt.Core.Scheduling.OccurrenceGenerator();
        var scheduler = new ListIt.Core.Scheduling.Scheduler();
        var occurrenceService = new ListIt.Core.Scheduling.OccurrenceService(service);
        using var runtime = new ListIt.Core.Scheduling.SchedulingRuntime(
            service, generator, scheduler, occurrenceService, clock: () => testTime);

        var vm = new MainViewModel(service, runtime);
        runtime.EvaluateNow(testTime);
        vm.SyncEvaluatedStates();

        vm.SelectedTask = vm.Tasks[0];
        Assert.True(vm.CanCompleteSelectedTask);

        // Act
        vm.DoneTaskCommand.Execute(null);

        // Assert: Task should be deleted from service and collection
        Assert.Empty(vm.Tasks);
        Assert.Null(vm.SelectedTask);
        Assert.Equal(1, service.DeleteCallCount);
    }

    [Fact]
    public void Commands_WithParameter_ExecuteOnSpecifiedItemWithoutPriorSelection()
    {
        // Arrange
        var testTime = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
        var service = new FakeTaskService();
        service.CreateRecurringTask("Task 1", new[] { new TimeOnly(9, 0) });
        service.CreateRecurringTask("Task 2", new[] { new TimeOnly(9, 0) });

        var generator = new ListIt.Core.Scheduling.OccurrenceGenerator();
        var scheduler = new ListIt.Core.Scheduling.Scheduler();
        var occurrenceService = new ListIt.Core.Scheduling.OccurrenceService(service);
        using var runtime = new ListIt.Core.Scheduling.SchedulingRuntime(
            service, generator, scheduler, occurrenceService, clock: () => testTime);

        var vm = new MainViewModel(service, runtime);
        runtime.EvaluateNow(testTime);
        vm.SyncEvaluatedStates();

        Assert.Null(vm.SelectedTask);

        // Act 1: Start work on Task 2 via command parameter
        vm.WorkTaskCommand.Execute(vm.Tasks[1]);

        // Assert 1
        Assert.False(vm.Tasks[0].IsWorking);
        Assert.True(vm.Tasks[1].IsWorking);

        // Act 2: Complete Task 1 via command parameter
        vm.DoneTaskCommand.Execute(vm.Tasks[0]);

        // Assert 2
        Assert.Equal(ListIt.Core.Scheduling.SchedulingState.Completed, vm.Tasks[0].SchedulingState);
        Assert.True(vm.Tasks[1].IsWorking);
    }
}
