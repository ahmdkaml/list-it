using System;
using ListIt.Core.Models;
using ListIt.UI.ViewModels;
using Xunit;

namespace ListIt.Tests.UI;

public class TaskItemViewModelTests
{
    [Fact]
    public void TaskItemViewModel_PassCountProperties_WhenZeroPasses()
    {
        var task = new ListitTask("Test", TaskType.Recurring, TimeSpan.FromHours(1), passes: 0);
        var vm = new TaskItemViewModel(task);

        Assert.Equal(0, vm.PassCount);
        Assert.False(vm.HasPasses);
        Assert.False(vm.HasMultiplePasses);
        Assert.Equal("Passes: 0", vm.PassesDisplay);
    }

    [Fact]
    public void TaskItemViewModel_PassCountProperties_WhenOnePass()
    {
        var task = new ListitTask("Test", TaskType.Recurring, TimeSpan.FromHours(1), passes: 1);
        var vm = new TaskItemViewModel(task);

        Assert.Equal(1, vm.PassCount);
        Assert.True(vm.HasPasses);
        Assert.False(vm.HasMultiplePasses);
        Assert.Equal("Passes: 1", vm.PassesDisplay);
    }

    [Fact]
    public void TaskItemViewModel_PassCountProperties_WhenMultiplePasses()
    {
        var task = new ListitTask("Test", TaskType.Recurring, TimeSpan.FromHours(1), passes: 3);
        var vm = new TaskItemViewModel(task);

        Assert.Equal(3, vm.PassCount);
        Assert.True(vm.HasPasses);
        Assert.True(vm.HasMultiplePasses);
        Assert.Equal("Passes: 3", vm.PassesDisplay);
    }

    [Fact]
    public void TaskItemViewModel_ScoreDisplay_FormatsCorrectly()
    {
        var task = new ListitTask("Test", TaskType.Recurring, TimeSpan.FromHours(1));
        var vm = new TaskItemViewModel(task);

        Assert.Equal("Score: 0", vm.ScoreDisplay);

        vm.Score = 42.7;
        Assert.Equal("Score: 43", vm.ScoreDisplay);

        vm.Score = 150.2;
        Assert.Equal("Score: 150", vm.ScoreDisplay);
    }

    [Fact]
    public void TaskItemViewModel_Refresh_RaisesPropertyChanges()
    {
        var task = new ListitTask("Test", TaskType.Recurring, TimeSpan.FromHours(1));
        var vm = new TaskItemViewModel(task);

        var changedProps = new System.Collections.Generic.List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null) changedProps.Add(e.PropertyName);
        };

        vm.Refresh();

        Assert.Contains(nameof(vm.PassCount), changedProps);
        Assert.Contains(nameof(vm.HasPasses), changedProps);
        Assert.Contains(nameof(vm.HasMultiplePasses), changedProps);
        Assert.Contains(nameof(vm.PassesDisplay), changedProps);
        Assert.Contains(nameof(vm.ScoreDisplay), changedProps);
        Assert.Contains(nameof(vm.DetailsDisplay), changedProps);
        Assert.Contains(nameof(vm.Title), changedProps);
        Assert.Contains(nameof(vm.Description), changedProps);
        Assert.Contains(nameof(vm.Urgency), changedProps);
    }
}
