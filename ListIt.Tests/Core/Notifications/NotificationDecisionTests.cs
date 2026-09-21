using System;
using ListIt.Core.Notifications;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationDecisionTests
{
    [Fact]
    public void Constructor_PreservesAllProperties()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var elapsed = TimeSpan.FromMinutes(15);
        var remaining = TimeSpan.FromMinutes(45);

        // Act
        var decision = new NotificationDecision(
            shouldNotify: true,
            taskId: taskId,
            occurrenceId: occurrenceId,
            urgency: 4,
            skipCount: 1,
            elapsed: elapsed,
            remaining: remaining,
            visualCategory: NotificationVisualCategory.High,
            opacity: 0.85);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(taskId, decision.TaskId);
        Assert.Equal(occurrenceId, decision.OccurrenceId);
        Assert.Equal(4, decision.Urgency);
        Assert.Equal(1, decision.SkipCount);
        Assert.Equal(elapsed, decision.Elapsed);
        Assert.Equal(remaining, decision.Remaining);
        Assert.Equal(NotificationVisualCategory.High, decision.VisualCategory);
        Assert.Equal(0.85, decision.Opacity);
    }

    [Fact]
    public void Notify_FactoryMethod_CreatesNotificationDecisionWithShouldNotifyTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var elapsed = TimeSpan.FromMinutes(5);
        var remaining = TimeSpan.FromMinutes(25);

        // Act
        var decision = NotificationDecision.Notify(
            taskId,
            occurrenceId,
            urgency: 3,
            skipCount: 0,
            elapsed: elapsed,
            remaining: remaining,
            visualCategory: NotificationVisualCategory.Medium,
            opacity: 0.9);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(taskId, decision.TaskId);
        Assert.Equal(occurrenceId, decision.OccurrenceId);
        Assert.Equal(3, decision.Urgency);
        Assert.Equal(0, decision.SkipCount);
        Assert.Equal(elapsed, decision.Elapsed);
        Assert.Equal(remaining, decision.Remaining);
        Assert.Equal(NotificationVisualCategory.Medium, decision.VisualCategory);
        Assert.Equal(0.9, decision.Opacity);
    }

    [Fact]
    public void DoNotNotify_FactoryMethod_CreatesDecisionWithShouldNotifyFalse()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();

        // Act
        var decision = NotificationDecision.DoNotNotify(taskId, occurrenceId);

        // Assert
        Assert.False(decision.ShouldNotify);
        Assert.Equal(taskId, decision.TaskId);
        Assert.Equal(occurrenceId, decision.OccurrenceId);
        Assert.Equal(0.0, decision.Opacity);
    }

    [Theory]
    [InlineData(NotificationVisualCategory.Low)]
    [InlineData(NotificationVisualCategory.Medium)]
    [InlineData(NotificationVisualCategory.High)]
    public void VisualCategory_SupportsAllDefinedCategories(NotificationVisualCategory category)
    {
        // Act
        var decision = NotificationDecision.Notify(
            Guid.NewGuid(),
            Guid.NewGuid(),
            urgency: 1,
            skipCount: 0,
            elapsed: TimeSpan.Zero,
            remaining: TimeSpan.Zero,
            visualCategory: category,
            opacity: 1.0);

        // Assert
        Assert.Equal(category, decision.VisualCategory);
    }
}
