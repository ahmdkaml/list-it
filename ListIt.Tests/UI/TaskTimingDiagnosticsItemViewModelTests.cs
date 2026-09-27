using System;
using ListIt.Core.Models;
using ListIt.UI.ViewModels;
using Xunit;

namespace ListIt.Tests.UI;

public class TaskTimingDiagnosticsItemViewModelTests
{
    [Fact]
    public void UpdateTick_FutureDeadline_CalculatesProgressAndCountdown()
    {
        var baseTime = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(2);
        var task = new ListitTask("Test Task", TaskType.Recurring, interval, startTime: baseTime);

        var vm = new TaskTimingDiagnosticsItemViewModel(task);

        // Advance 30 minutes into the 2-hour interval (25% elapsed)
        var currentTime = baseTime.AddMinutes(30);
        vm.UpdateTick(currentTime);

        Assert.Equal(25.0, vm.ProgressPercentage);
        Assert.Equal("25.0%", vm.ProgressDisplay);
        Assert.Equal("01h 30m 00s", vm.DeadlineCountdown);
        Assert.Equal("Scheduled", vm.StatusDisplay);
    }

    [Fact]
    public void UpdateTick_OverdueDeadline_CalculatesOverdueStatusAndCountdown()
    {
        var baseTime = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var task = new ListitTask("Test Task", TaskType.Recurring, interval, startTime: baseTime);

        var vm = new TaskTimingDiagnosticsItemViewModel(task);

        // Advance 10 minutes past the 1-hour deadline
        var currentTime = baseTime.AddHours(1).AddMinutes(10);
        vm.UpdateTick(currentTime);

        Assert.Equal(100.0, vm.ProgressPercentage);
        Assert.Equal("100.0%", vm.ProgressDisplay);
        Assert.Equal("Overdue by 00h 10m 00s", vm.DeadlineCountdown);
        Assert.Equal("Overdue", vm.StatusDisplay);
    }

    [Fact]
    public void UpdateTick_HalvingAlerts_IdentifiesNextAlertCorrectly()
    {
        var baseTime = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromSeconds(1000);
        var task = new ListitTask("Test Task", TaskType.Recurring, interval, startTime: baseTime);

        var vm = new TaskTimingDiagnosticsItemViewModel(task);

        // At 600s elapsed (60%): crossed 50% (500s), next is 75% (750s)
        var currentTime = baseTime.AddSeconds(600);
        vm.UpdateTick(currentTime);

        Assert.NotNull(vm.NextHalvingAlertUtc);
        Assert.Equal(baseTime.AddSeconds(750), vm.NextHalvingAlertUtc.Value);
        Assert.Equal(0.75, vm.NextHalvingThreshold);
        Assert.Equal("00h 02m 30s", vm.NextHalvingCountdown);
        Assert.Equal("1 / 5 halving steps elapsed", vm.AlertStepsSummary);
    }

    [Fact]
    public void UpdateTick_PastAllHalvingAlerts_ShowsAllPassed()
    {
        var baseTime = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromSeconds(1000);
        var task = new ListitTask("Test Task", TaskType.Recurring, interval, startTime: baseTime);

        var vm = new TaskTimingDiagnosticsItemViewModel(task);

        // At 980s elapsed (98%): crossed all 5 thresholds (up to 96.875%), before deadline (1000s)
        var currentTime = baseTime.AddSeconds(980);
        vm.UpdateTick(currentTime);

        Assert.Null(vm.NextHalvingAlertUtc);
        Assert.Equal("All halving alerts passed (next is deadline)", vm.NextHalvingAlertDisplay);
        Assert.Equal("00h 00m 20s", vm.NextHalvingCountdown);
        Assert.Equal("5 / 5 halving steps elapsed", vm.AlertStepsSummary);
    }

    [Fact]
    public void PassCount_Properties_ReflectValuesCorrectly()
    {
        var task0 = new ListitTask("Task 0", TaskType.Recurring, TimeSpan.FromHours(1), passes: 0);
        var vm0 = new TaskTimingDiagnosticsItemViewModel(task0);
        Assert.False(vm0.HasPasses);
        Assert.False(vm0.HasMultiplePasses);
        Assert.Equal("Passes: 0", vm0.PassesDisplay);

        var task1 = new ListitTask("Task 1", TaskType.Recurring, TimeSpan.FromHours(1), passes: 1);
        var vm1 = new TaskTimingDiagnosticsItemViewModel(task1);
        Assert.True(vm1.HasPasses);
        Assert.False(vm1.HasMultiplePasses);
        Assert.Equal("Passes: 1", vm1.PassesDisplay);

        var task2 = new ListitTask("Task 2", TaskType.Recurring, TimeSpan.FromHours(1), passes: 2);
        var vm2 = new TaskTimingDiagnosticsItemViewModel(task2);
        Assert.True(vm2.HasPasses);
        Assert.True(vm2.HasMultiplePasses);
        Assert.Equal("Passes: 2", vm2.PassesDisplay);
    }

    [Fact]
    public void WorkingState_OverridesStatusDisplay()
    {
        var task = new ListitTask("Task", TaskType.Recurring, TimeSpan.FromHours(1));
        var vm = new TaskTimingDiagnosticsItemViewModel(task)
        {
            IsWorking = true
        };

        vm.UpdateTick(DateTime.UtcNow);
        Assert.Equal("Working", vm.StatusDisplay);
    }
}
