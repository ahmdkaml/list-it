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
        private readonly List<ListitTask> _tasks = new();

        public int DeleteCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int CreateCallCount { get; private set; }

        public FakeTaskService(IEnumerable<ListitTask>? initialTasks = null)
        {
            if (initialTasks != null)
            {
                _tasks.AddRange(initialTasks);
            }
        }

        public IReadOnlyList<ListitTask> GetAllTasks() => _tasks.ToList().AsReadOnly();

        public ListitTask? GetTask(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

        public ListitTask CreateTask(
            string title,
            TaskType type = TaskType.Recurring,
            TimeSpan? interval = null,
            string description = "",
            int urgency = 1,
            DateTime? startTime = null,
            int requiredCompletions = 1,
            bool bypassPrioritySuppression = false)
        {
            CreateCallCount++;
            var task = new ListitTask(
                title: title,
                type: type,
                interval: interval,
                description: description,
                urgency: urgency,
                startTime: startTime,
                requiredCompletions: requiredCompletions,
                bypassPrioritySuppression: bypassPrioritySuppression);
            _tasks.Add(task);
            return task;
        }

        public void UpdateTask(ListitTask task)
        {
            UpdateCallCount++;
            var idx = _tasks.FindIndex(t => t.Id == task.Id);
            if (idx >= 0) _tasks[idx] = task;
        }

        public bool DeleteTask(Guid id)
        {
            DeleteCallCount++;
            return _tasks.RemoveAll(t => t.Id == id) > 0;
        }
    }

    [Fact]
    public void Constructor_LoadsTasksFromService()
    {
        // Arrange
        var initial = new ListitTask[]
        {
            new ListitTask("Task 1", TaskType.Recurring),
            new ListitTask("Task 2", TaskType.Finite, requiredCompletions: 2)
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
        var task = new ListitTask("Target Task", TaskType.Finite, description: "Desc", urgency: 4, requiredCompletions: 3);
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
        var task = new ListitTask("Task to Delete", TaskType.Finite, requiredCompletions: 1);
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
        var task = new ListitTask("Task to Keep", TaskType.Finite, requiredCompletions: 1);
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

        service.CreateTask("New Task", TaskType.Finite, requiredCompletions: 1);

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
        service.CreateTask("Morning Review", TaskType.Recurring, TimeSpan.FromDays(1), startTime: testTime);

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
        service.CreateTask("Daily Sync", TaskType.Recurring, TimeSpan.FromDays(1), startTime: testTime);

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
        service.CreateTask("Finish Report", TaskType.Finite, TimeSpan.FromHours(1), startTime: testTime.AddHours(-1), requiredCompletions: 1);

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
        service.CreateTask("Task 1", TaskType.Recurring, TimeSpan.FromDays(1), startTime: testTime);
        service.CreateTask("Task 2", TaskType.Recurring, TimeSpan.FromDays(1), startTime: testTime);

        var generator = new ListIt.Core.Scheduling.OccurrenceGenerator();
        var scheduler = new ListIt.Core.Scheduling.Scheduler();
        var occurrenceService = new ListIt.Core.Scheduling.OccurrenceService(service);
        using var runtime = new ListIt.Core.Scheduling.SchedulingRuntime(
            service, generator, scheduler, occurrenceService, clock: () => testTime);

        var vm = new MainViewModel(service, runtime);
        runtime.EvaluateNow(testTime);
        vm.SyncEvaluatedStates();

        Assert.Null(vm.SelectedTask);
        var task1 = vm.Tasks[0];
        var task2 = vm.Tasks[1];

        // Act 1: Start work on Task 2 via command parameter
        vm.WorkTaskCommand.Execute(task2);

        // Assert 1: task2 is working and dynamically boosted to top
        Assert.False(task1.IsWorking);
        Assert.True(task2.IsWorking);
        Assert.Same(task2, vm.Tasks[0]);

        // Act 2: Complete Task 1 via command parameter
        vm.DoneTaskCommand.Execute(task1);

        // Assert 2
        Assert.Equal(ListIt.Core.Scheduling.SchedulingState.Completed, task1.SchedulingState);
        Assert.True(task2.IsWorking);
    }

    [Fact]
    public void SortTasks_OrdersTasksByScoreDescending_AndPreservesSelectedTask()
    {
        // Arrange: Task 1 low urgency, Task 2 high urgency
        var initial = new ListitTask[]
        {
            new ListitTask("Low Urgency", TaskType.Recurring, urgency: 1),
            new ListitTask("High Urgency", TaskType.Recurring, urgency: 5)
        };
        var service = new FakeTaskService(initial);
        var vm = new MainViewModel(service);

        // Act: Task with higher urgency should have higher score and be at index 0
        Assert.Equal("High Urgency", vm.Tasks[0].Title);
        Assert.Equal("Low Urgency", vm.Tasks[1].Title);

        // Select the lower one
        vm.SelectedTask = vm.Tasks[1];
        Assert.Equal("Low Urgency", vm.SelectedTask.Title);

        // Re-sort
        vm.SortTasks();

        // Selection should be preserved
        Assert.NotNull(vm.SelectedTask);
        Assert.Equal("Low Urgency", vm.SelectedTask.Title);
    }
}
