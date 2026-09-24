using System;
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
        Assert.Equal("1d", vm.IntervalString);
        Assert.Equal(1, vm.RequiredCompletions);
    }

    [Fact]
    public void LoadForEdit_RecurringTask_PopulatesProperties()
    {
        // Arrange
        var task = new ListitTask("Morning Sync", TaskType.Recurring, TimeSpan.FromHours(12), "Daily standup", 3);
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
        Assert.Equal("12h", vm.IntervalString);
    }

    [Fact]
    public void LoadForEdit_FiniteTask_PopulatesProperties()
    {
        // Arrange
        var task = new ListitTask("Submit Report", TaskType.Finite, TimeSpan.FromDays(2), "Monthly accounting", 5, requiredCompletions: 5, currentCompletions: 2);
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
        Assert.Equal("2d", vm.IntervalString);
    }

    [Fact]
    public void SaveCommand_ValidRecurringTask_RaisesTaskSaved()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.Title = "Valid Recurring";
        vm.Urgency = 2;
        vm.IntervalString = "1d";

        ListitTask? savedTask = null;
        vm.TaskSaved += (s, t) => savedTask = t;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.NotNull(savedTask);
        Assert.Equal("Valid Recurring", savedTask.Title);
        Assert.Equal(2, savedTask.Urgency);
        Assert.Equal(TaskType.Recurring, savedTask.Type);
        Assert.Equal(TimeSpan.FromDays(1), savedTask.Interval);
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
        vm.IntervalString = "6h";

        ListitTask? savedTask = null;
        vm.TaskSaved += (s, t) => savedTask = t;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.NotNull(savedTask);
        Assert.Equal("Valid Finite", savedTask.Title);
        Assert.Equal(4, savedTask.RequiredCompletions);
        Assert.Equal(6, savedTask.Urgency);
        Assert.Equal(TaskType.Finite, savedTask.Type);
        Assert.Equal(TimeSpan.FromHours(6), savedTask.Interval);
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

    [Theory]
    [InlineData("invalid-interval")]
    [InlineData("-5d")]
    [InlineData("0h")]
    public void SaveCommand_InvalidInterval_SetsErrorMessage(string invalidInterval)
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.Title = "Task";
        vm.IntervalString = invalidInterval;

        var wasSaved = false;
        vm.TaskSaved += (s, t) => wasSaved = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(wasSaved);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void SaveCommand_FiniteTask_WithInvalidRequiredCompletions_SetsErrorMessage()
    {
        // Arrange
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.SelectedType = TaskType.Finite;
        vm.Title = "Finite Zero Count";
        vm.RequiredCompletions = 0;

        var wasSaved = false;
        vm.TaskSaved += (s, t) => wasSaved = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(wasSaved);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void LoadForCreate_SetsBypassPrioritySuppressionToFalse()
    {
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        Assert.False(vm.BypassPrioritySuppression);
    }

    [Fact]
    public void LoadForEdit_SetsBypassPrioritySuppressionFromTask()
    {
        var task = new ListitTask("Urgent", TaskType.Recurring, bypassPrioritySuppression: true);
        var vm = new TaskEditorViewModel();

        vm.LoadForEdit(task);

        Assert.True(vm.BypassPrioritySuppression);
    }

    [Fact]
    public void SaveCommand_PropagatesBypassPrioritySuppression_ToCreatedTask()
    {
        var vm = new TaskEditorViewModel();
        vm.LoadForCreate();
        vm.Title = "Bypass Enabled";
        vm.BypassPrioritySuppression = true;

        ListitTask? savedTask = null;
        vm.TaskSaved += (s, t) => savedTask = t;

        vm.SaveCommand.Execute(null);

        Assert.NotNull(savedTask);
        Assert.True(savedTask.BypassPrioritySuppression);
    }
}
