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
}
