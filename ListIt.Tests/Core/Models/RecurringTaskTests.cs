using System;
using System.Collections.Generic;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class RecurringTaskTests
{
    [Fact]
    public void OneAssignedTime_IsAccepted()
    {
        // Arrange
        var time = new TimeOnly(9, 0);

        // Act
        var task = new RecurringTask("Morning Review", new[] { time });

        // Assert
        Assert.Single(task.AssignedTimes);
        Assert.Equal(time, task.AssignedTimes[0]);
    }

    [Fact]
    public void MultipleAssignedTimes_AreAccepted()
    {
        // Arrange
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(13, 0), new TimeOnly(18, 0) };

        // Act
        var task = new RecurringTask("Multi-time Review", times);

        // Assert
        Assert.Equal(3, task.AssignedTimes.Count);
        Assert.Equal(times, task.AssignedTimes);
    }

    [Fact]
    public void EmptyAssignedTimes_IsRejected()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RecurringTask("Review", Array.Empty<TimeOnly>()));
    }

    [Fact]
    public void NullAssignedTimes_IsRejected()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RecurringTask("Review", null!));
    }

    [Fact]
    public void DuplicateAssignedTimes_AreNormalizedAndDeduplicated()
    {
        // Arrange
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(9, 0), new TimeOnly(18, 0), new TimeOnly(9, 0) };

        // Act
        var task = new RecurringTask("Review", times);

        // Assert
        Assert.Equal(2, task.AssignedTimes.Count);
        Assert.Equal(new TimeOnly(9, 0), task.AssignedTimes[0]);
        Assert.Equal(new TimeOnly(18, 0), task.AssignedTimes[1]);
    }

    [Fact]
    public void AssignedTimes_AreSortedConsistently()
    {
        // Arrange (unsorted input)
        var times = new[] { new TimeOnly(18, 0), new TimeOnly(9, 0), new TimeOnly(13, 0) };

        // Act
        var task = new RecurringTask("Review", times);

        // Assert
        Assert.Equal(new TimeOnly(9, 0), task.AssignedTimes[0]);
        Assert.Equal(new TimeOnly(13, 0), task.AssignedTimes[1]);
        Assert.Equal(new TimeOnly(18, 0), task.AssignedTimes[2]);
    }

    [Fact]
    public void SetAssignedTimes_UpdatesAndEnforcesInvariants()
    {
        // Arrange
        var task = new RecurringTask("Review", new[] { new TimeOnly(9, 0) });

        // Act
        task.SetAssignedTimes(new[] { new TimeOnly(17, 30), new TimeOnly(8, 0) });

        // Assert
        Assert.Equal(2, task.AssignedTimes.Count);
        Assert.Equal(new TimeOnly(8, 0), task.AssignedTimes[0]);
        Assert.Equal(new TimeOnly(17, 30), task.AssignedTimes[1]);

        // Assert rejection of empty update
        Assert.Throws<ArgumentException>(() => task.SetAssignedTimes(Array.Empty<TimeOnly>()));
    }
}
