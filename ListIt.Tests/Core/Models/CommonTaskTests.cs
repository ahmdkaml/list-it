using System;
using ListIt.Core.Models;
using Xunit;

namespace ListIt.Tests.Core.Models;

public class CommonTaskTests
{
    private class TestTask : TaskBase
    {
        public override TaskType Type => TaskType.Finite;

        public TestTask(string title, string description = "", int urgency = 1)
            : base(title, description, urgency)
        {
        }

        public TestTask(Guid id, string title, string description, int urgency, DateTime createdAt)
            : base(id, title, description, urgency, createdAt)
        {
        }
    }

    [Fact]
    public void ValidTask_CanBeCreated_WithDefaults()
    {
        // Act
        var task = new TestTask("Buy groceries");

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Buy groceries", task.Title);
        Assert.Equal(string.Empty, task.Description);
        Assert.Equal(1, task.Urgency);
        Assert.Equal(DateTimeKind.Utc, task.CreatedAt.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void EmptyOrWhitespaceTitle_IsRejected_OnCreation(string? invalidTitle)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TestTask(invalidTitle!));
        Assert.Equal("title", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceTitle_IsRejected_OnUpdate(string? invalidTitle)
    {
        // Arrange
        var task = new TestTask("Initial title");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => task.SetTitle(invalidTitle!));
        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void Title_IsNormalized_TrimsSurroundingWhitespace()
    {
        // Act
        var task = new TestTask("   Study for exam   ");

        // Assert
        Assert.Equal("Study for exam", task.Title);

        // Act on update
        task.SetTitle("   New Title   ");
        Assert.Equal("New Title", task.Title);
    }

    [Fact]
    public void Description_Null_DefaultsToEmptyString()
    {
        // Act
        var task = new TestTask("Task", null!);

        // Assert
        Assert.Equal(string.Empty, task.Description);

        // Update with null
        task.SetDescription(null!);
        Assert.Equal(string.Empty, task.Description);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Urgency_ValidRange_IsAccepted(int urgency)
    {
        // Act
        var task = new TestTask("Task", urgency: urgency);

        // Assert
        Assert.Equal(urgency, task.Urgency);

        // Update
        task.SetUrgency(urgency);
        Assert.Equal(urgency, task.Urgency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(100)]
    public void Urgency_OutOfRange_IsRejected(int invalidUrgency)
    {
        // Act & Assert on creation
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestTask("Task", urgency: invalidUrgency));

        // Act & Assert on update
        var task = new TestTask("Task");
        Assert.Throws<ArgumentOutOfRangeException>(() => task.SetUrgency(invalidUrgency));
    }

    [Fact]
    public void CreatedAt_IsStoredAsUtc()
    {
        // Act
        var task = new TestTask("Task");

        // Assert
        Assert.Equal(DateTimeKind.Utc, task.CreatedAt.Kind);
        Assert.True(task.CreatedAt <= DateTime.UtcNow);
        Assert.True(task.CreatedAt >= DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void NonUtcCreatedAt_ThrowsArgumentException()
    {
        // Arrange
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TestTask(Guid.NewGuid(), "Task", "", 1, localTime));
    }

    [Fact]
    public void Id_IsGeneratedAndRemainsStableAcrossUpdates()
    {
        // Arrange
        var task = new TestTask("Initial title", "Initial desc", 1);
        var initialId = task.Id;

        // Act
        task.UpdateDetails("Updated title", "Updated desc");
        task.SetUrgency(5);

        // Assert
        Assert.Equal(initialId, task.Id);
    }
}
