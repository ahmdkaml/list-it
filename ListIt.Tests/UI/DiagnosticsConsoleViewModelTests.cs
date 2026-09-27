using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Services;
using ListIt.UI.ViewModels;
using Xunit;

namespace ListIt.Tests.UI;

public class DiagnosticsConsoleViewModelTests
{
    private class FakeTaskService : ITaskService
    {
        private readonly List<ListitTask> _tasks = new();

        public FakeTaskService(params ListitTask[] tasks)
        {
            _tasks.AddRange(tasks);
        }

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
            var task = new ListitTask(
                title: title,
                type: type,
                interval: interval ?? TimeSpan.FromDays(1),
                description: description,
                urgency: urgency,
                startTime: startTime,
                requiredCompletions: requiredCompletions,
                bypassPrioritySuppression: bypassPrioritySuppression);
            _tasks.Add(task);
            return task;
        }

        public ListitTask? GetTask(Guid id) => _tasks.Find(t => t.Id == id);
        public IReadOnlyList<ListitTask> GetAllTasks() => _tasks.ToList().AsReadOnly();

        public void UpdateTask(ListitTask task)
        {
            var index = _tasks.FindIndex(t => t.Id == task.Id);
            if (index >= 0)
            {
                _tasks[index] = task;
            }
        }

        public bool DeleteTask(Guid id) => _tasks.RemoveAll(t => t.Id == id) > 0;
    }

    [Fact]
    public void LoadTasks_PopulatesTasksFromService()
    {
        var task1 = new ListitTask("Task A", TaskType.Recurring, TimeSpan.FromHours(1));
        var task2 = new ListitTask("Task B", TaskType.Finite, TimeSpan.FromHours(4));
        var fakeService = new FakeTaskService(task1, task2);

        using var vm = new DiagnosticsConsoleViewModel(fakeService, startTimer: false);

        Assert.Equal(2, vm.Tasks.Count);
        Assert.Contains(vm.Tasks, t => t.Title == "Task A");
        Assert.Contains(vm.Tasks, t => t.Title == "Task B");
    }

    [Fact]
    public void Tick_UpdatesAllTaskDiagnosticItems()
    {
        var baseTime = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);
        var task = new ListitTask("Task A", TaskType.Recurring, TimeSpan.FromHours(2), startTime: baseTime);
        var fakeService = new FakeTaskService(task);

        using var vm = new DiagnosticsConsoleViewModel(fakeService, startTimer: false);

        var item = Assert.Single(vm.Tasks);
        var checkTime = baseTime.AddHours(1); // 50% elapsed
        vm.Tick(checkTime);

        Assert.Equal(50.0, item.ProgressPercentage);
        Assert.Equal("50.0%", item.ProgressDisplay);
        Assert.Equal("01h 00m 00s", item.DeadlineCountdown);
    }

    [Fact]
    public void CloseCommand_RaisesRequestClose()
    {
        var fakeService = new FakeTaskService();
        using var vm = new DiagnosticsConsoleViewModel(fakeService, startTimer: false);

        bool closeFired = false;
        vm.RequestClose += () => closeFired = true;

        vm.CloseCommand.Execute(null);

        Assert.True(closeFired);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var fakeService = new FakeTaskService();
        var vm = new DiagnosticsConsoleViewModel(fakeService, startTimer: false);

        vm.Dispose();
        vm.Dispose(); // second call should not throw
    }
}
