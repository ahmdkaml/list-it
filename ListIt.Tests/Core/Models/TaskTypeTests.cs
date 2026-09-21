using System;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class TaskTypeTests
{
    [Fact]
    public void RecurringTask_TypeIsRecurring()
    {
        // Arrange
        var task = new RecurringTask("Morning Standup", new[] { new TimeOnly(9, 30) });

        // Assert
        Assert.Equal(TaskType.Recurring, task.Type);
    }

    [Fact]
    public void FiniteTask_TypeIsFinite()
    {
        // Arrange
        var task = new FiniteTask("Submit Expense Report", requiredCompletions: 1);

        // Assert
        Assert.Equal(TaskType.Finite, task.Type);
    }

    [Fact]
    public void TaskType_CannotBeMutatedIndependently()
    {
        // Verify via polymorphism that Type is read-only and governed by the concrete class
        TaskBase recurring = new RecurringTask("Task", new[] { new TimeOnly(10, 0) });
        TaskBase finite = new FiniteTask("Task", requiredCompletions: 1);

        Assert.Equal(TaskType.Recurring, recurring.Type);
        Assert.Equal(TaskType.Finite, finite.Type);

        // There is no public setter on Type
        var typeProp = typeof(TaskBase).GetProperty(nameof(TaskBase.Type));
        Assert.NotNull(typeProp);
        Assert.Null(typeProp.GetSetMethod());
    }
}
