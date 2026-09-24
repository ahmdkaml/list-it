using System;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class TaskTypeTests
{
    [Fact]
    public void Task_TypeDefaultsToRecurring()
    {
        var task = new ListitTask("Morning Standup");
        Assert.Equal(TaskType.Recurring, task.Type);
    }

    [Fact]
    public void FiniteTask_TypeIsFinite()
    {
        var task = new ListitTask("Submit Expense Report", type: TaskType.Finite, requiredCompletions: 1);
        Assert.Equal(TaskType.Finite, task.Type);
    }

    [Fact]
    public void TaskType_CanBeSwitchedViaDomainMethod()
    {
        var task = new ListitTask("Task", type: TaskType.Recurring);
        Assert.Equal(TaskType.Recurring, task.Type);

        task.SetType(TaskType.Finite);
        Assert.Equal(TaskType.Finite, task.Type);
        Assert.True(task.RequiredCompletions >= 1);
    }
}
