using System;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class FiniteTaskTests
{
    [Fact]
    public void RequiredCompletion_OfOne_IsAccepted()
    {
        // Act
        var task = new FiniteTask("Single run task", requiredCompletions: 1);

        // Assert
        Assert.Equal(1, task.RequiredCompletions);
        Assert.Equal(0, task.CurrentCompletions);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(100)]
    public void RequiredCompletion_LargerCount_IsAccepted(int required)
    {
        // Act
        var task = new FiniteTask("Multi run task", requiredCompletions: required);

        // Assert
        Assert.Equal(required, task.RequiredCompletions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void RequiredCompletion_ZeroOrNegative_IsRejected(int invalidRequired)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FiniteTask("Task", invalidRequired));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    public void CurrentCompletion_Negative_IsRejected(int invalidCurrent)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FiniteTask("Task", requiredCompletions: 3, currentCompletions: invalidCurrent));
    }

    [Fact]
    public void CurrentCompletion_CannotExceedRequired()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FiniteTask("Task", requiredCompletions: 3, currentCompletions: 4));
    }

    [Fact]
    public void ValidPartialCompletion_IsAccepted()
    {
        // Act
        var task = new FiniteTask("Task", requiredCompletions: 5, currentCompletions: 3);

        // Assert
        Assert.Equal(5, task.RequiredCompletions);
        Assert.Equal(3, task.CurrentCompletions);
    }

    [Fact]
    public void RecordCompletion_IncrementsCurrentCount()
    {
        // Arrange
        var task = new FiniteTask("Task", requiredCompletions: 2, currentCompletions: 0);

        // Act
        task.RecordCompletion();

        // Assert
        Assert.Equal(1, task.CurrentCompletions);

        // Act again
        task.RecordCompletion();
        Assert.Equal(2, task.CurrentCompletions);

        // Exceeding throws InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => task.RecordCompletion());
    }

    [Fact]
    public void ReachingRequiredCount_DoesNotAlterDomainIdentityOrDelete()
    {
        // Arrange
        var task = new FiniteTask("Finish Report", requiredCompletions: 1, currentCompletions: 0);
        var id = task.Id;

        // Act
        task.RecordCompletion();

        // Assert
        Assert.Equal(1, task.CurrentCompletions);
        Assert.Equal(1, task.RequiredCompletions);
        Assert.Equal(id, task.Id);
        Assert.Equal("Finish Report", task.Title);
    }

    [Fact]
    public void SetRequiredCompletions_EnforcesInvariants()
    {
        // Arrange
        var task = new FiniteTask("Task", requiredCompletions: 3, currentCompletions: 2);

        // Act - increase required
        task.SetRequiredCompletions(5);
        Assert.Equal(5, task.RequiredCompletions);

        // Reject setting below current completions
        Assert.Throws<ArgumentException>(() => task.SetRequiredCompletions(1));

        // Reject zero or negative
        Assert.Throws<ArgumentOutOfRangeException>(() => task.SetRequiredCompletions(0));
    }
}
