using System;
using System.Linq;
using ListIt.Core.Models;
using ListIt.UI.ViewModels;
using Xunit;

namespace ListIt.Tests.UI;

public class TaskEditorViewModelTests
{
    [Fact]
    public void LoadForCreate_InitializesDefaultValues()
    {
        // Act
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();

        // Assert
        Assert.False(vm.IsEditing);
        Assert.True(vm.IsTypeSelectionEnabled);
        Assert.Equal(TaskType.Recurring, vm.SelectedType);
        Assert.True(vm.IsRecurring);
        Assert.False(vm.IsFinite);
        Assert.Equal(string.Empty, vm.Title);
        Assert.Equal(1, vm.Urgency);
        Assert.Single(vm.AssignedTimes);
        Assert.Equal(new TimeOnly(9, 0), vm.AssignedTimes[0]);
    }

    [Fact]
    public void LoadForEdit_RecurringTask_PopulatesProperties()
    {
        // Arrange
        var times = new[] { new TimeOnly(8, 0), new TimeOnly(17, 30) };
        var task = new RecurringTask("Morning Sync", times, "Daily standup", 3);
        var vm = new TaskEditorViewModel();

        // Act
        vm.LoadForEdit(task);

        // Assert
        Assert.True(vm.IsEditing);
        Assert.False(vm.IsTypeSelectionEnabled);
        Assert.Equal("Morning Sync", vm.Title);
        Assert.Equal("Daily standup", vm.Description);
        Assert.Equal(3, vm.Urgency);
        Assert.Equal(TaskType.Recurring, vm.SelectedType);
        Assert.Equal(2, vm.AssignedTimes.Count);
    }

    [Fact]
    public void LoadForEdit_FiniteTask_PopulatesProperties()
    {
        // Arrange
        var task = new FiniteTask("Submit Report", 5, 2, "Monthly accounting", 5);
        var vm = new TaskEditorViewModel();

        // Act
        vm.LoadForEdit(task);

        // Assert
        Assert.True(vm.IsEditing);
        Assert.False(vm.IsTypeSelectionEnabled);
        Assert.Equal("Submit Report", vm.Title);
        Assert.Equal("Monthly accounting", vm.Description);
        Assert.Equal(5, vm.Urgency);
        Assert.Equal(TaskType.Finite, vm.SelectedType);
        Assert.Equal(5, vm.RequiredCompletions);
    }

    [Fact]
    public void AddTimeCommand_AddsValidTime_AndRejectsInvalidOrDuplicate()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        Assert.Single(vm.AssignedTimes); // Has 09:00

        // Act - Add new time
        vm.NewTimeString = "14:30";
        vm.AddTimeCommand.Execute(null);

        // Assert
        Assert.Equal(2, vm.AssignedTimes.Count);
        Assert.Contains(new TimeOnly(14, 30), vm.AssignedTimes);

        // Act - Add duplicate time
        vm.NewTimeString = "14:30";
        vm.AddTimeCommand.Execute(null);
        Assert.Equal(2, vm.AssignedTimes.Count);
        Assert.NotNull(vm.ErrorMessage);

        // Act - Add invalid format
        vm.NewTimeString = "not-a-time";
        vm.AddTimeCommand.Execute(null);
        Assert.Equal(2, vm.AssignedTimes.Count);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void RemoveTimeCommand_RemovesSelectedTime()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        var time = vm.AssignedTimes.First();

        // Act
        vm.RemoveTimeCommand.Execute(time);

        // Assert
        Assert.Empty(vm.AssignedTimes);
    }

    [Fact]
    public void SaveCommand_ValidRecurringTask_RaisesTaskSaved()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.Title = "Valid Recurring";
        vm.Urgency = 2;

        TaskBase? savedTask = null;
        vm.TaskSaved += (s, t) => savedTask = t;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.NotNull(savedTask);
        var recurring = Assert.IsType<RecurringTask>(savedTask);
        Assert.Equal("Valid Recurring", recurring.Title);
        Assert.Equal(2, recurring.Urgency);
    }

    [Fact]
    public void SaveCommand_ValidFiniteTask_RaisesTaskSaved()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.SelectedType = TaskType.Finite;
        vm.Title = "Valid Finite";
        vm.RequiredCompletions = 4;
        vm.Urgency = 6;

        TaskBase? savedTask = null;
        vm.TaskSaved += (s, t) => savedTask = t;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.NotNull(savedTask);
        var finite = Assert.IsType<FiniteTask>(savedTask);
        Assert.Equal("Valid Finite", finite.Title);
        Assert.Equal(4, finite.RequiredCompletions);
        Assert.Equal(6, finite.Urgency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SaveCommand_EmptyTitle_SetsErrorMessage_AndDoesNotSave(string invalidTitle)
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.Title = invalidTitle;

        var wasSaved = false;
        vm.TaskSaved += (s, t) => wasSaved = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(wasSaved);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void SaveCommand_RecurringWithNoTimes_SetsErrorMessage()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.Title = "Task";
        vm.AssignedTimes.Clear();

        var wasSaved = false;
        vm.TaskSaved += (s, t) => wasSaved = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(wasSaved);
        Assert.NotNull(vm.ErrorMessage);
    }
}
