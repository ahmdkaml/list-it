using System;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using Xunit;

namespace ListIt.Tests.Core.Notifications;

public class NotificationPresentationPolicyTests
{
    private readonly NotificationPresentationPolicy _policy = new();

    [Theory]
    [InlineData(1, NotificationVisualCategory.Low)]
    [InlineData(2, NotificationVisualCategory.Low)]
    [InlineData(3, NotificationVisualCategory.Medium)]
    [InlineData(4, NotificationVisualCategory.Medium)]
    [InlineData(5, NotificationVisualCategory.High)]
    [InlineData(6, NotificationVisualCategory.High)]
    public void GetVisualCategory_ValidUrgency_MapsExpectedCategory(int urgency, NotificationVisualCategory expected)
    {
        var category = _policy.GetVisualCategory(urgency);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(100)]
    public void GetVisualCategory_InvalidUrgency_ThrowsArgumentOutOfRangeException(int invalidUrgency)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _policy.GetVisualCategory(invalidUrgency));
    }

    [Theory]
    [InlineData(0, 0.50)]
    [InlineData(1, 0.50)]
    [InlineData(2, 0.625)]
    [InlineData(3, 0.75)]
    [InlineData(4, 0.875)]
    [InlineData(5, 1.00)]
    [InlineData(6, 1.00)]
    [InlineData(10, 1.00)]
    public void CalculateOpacity_LinearSkipProgression_MatchesSpecification(int skipCount, double expectedOpacity)
    {
        var opacity = _policy.CalculateOpacity(skipCount);
        Assert.Equal(expectedOpacity, opacity, precision: 3);
    }

    [Fact]
    public void CalculateOpacity_NegativeSkip_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _policy.CalculateOpacity(-1));
    }

    [Fact]
    public void CalculateOpacity_RangeClamp_AlwaysBetween0Point5And1Point0()
    {
        for (int skips = 0; skips <= 20; skips++)
        {
            var opacity = _policy.CalculateOpacity(skips);
            Assert.InRange(opacity, 0.5, 1.0);
        }
    }

    [Fact]
    public void Evaluate_AllowedOpportunity_PreservesTimesAndSetsPresentationMetadata()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var currentTime = scheduledAt.AddMinutes(45);
        var task = new RecurringTask("Urgency 4 Task", new[] { new TimeOnly(10, 0) }, urgency: 4);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue, skipCount: 3);
        var opportunity = new NotificationOpportunity(task.Id, occurrence.OccurrenceId, 0, 0.5, currentTime);
        var suppression = NotificationSuppressionResult.Allowed("AllowedNoActiveWork");

        // Act
        var decision = _policy.Evaluate(context, opportunity, suppression);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(task.Id, decision.TaskId);
        Assert.Equal(occurrence.OccurrenceId, decision.OccurrenceId);
        Assert.Equal(4, decision.Urgency);
        Assert.Equal(3, decision.SkipCount);
        Assert.Equal(TimeSpan.FromMinutes(45), decision.Elapsed);
        Assert.Equal(TimeSpan.Zero, decision.Remaining);
        Assert.Equal(NotificationVisualCategory.Medium, decision.VisualCategory);
        Assert.Equal(0.75, decision.Opacity, precision: 3);
        Assert.Same(suppression, decision.SuppressionResult);
    }

    [Fact]
    public void Evaluate_SuppressedOpportunity_ProducesDoNotNotifyDecision()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var currentTime = scheduledAt.AddMinutes(60);
        var task = new RecurringTask("Urgency 2 Task", new[] { new TimeOnly(10, 0) }, urgency: 2);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);
        var opportunity = new NotificationOpportunity(task.Id, occurrence.OccurrenceId, 0, 1.0, currentTime);
        var suppression = NotificationSuppressionResult.Suppressed("SuppressedByPriority", Guid.NewGuid(), 5);

        // Act
        var decision = _policy.Evaluate(context, opportunity, suppression);

        // Assert
        Assert.False(decision.ShouldNotify);
        Assert.Equal(0.0, decision.Opacity);
        Assert.Equal(NotificationVisualCategory.Low, decision.VisualCategory);
        Assert.Same(suppression, decision.SuppressionResult);
    }

    [Theory]
    [InlineData(OccurrenceStatus.Completed)]
    [InlineData(OccurrenceStatus.Missed)]
    public void Evaluate_TerminalOccurrences_DoNotProducePresentationDecision(OccurrenceStatus terminalStatus)
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var currentTime = scheduledAt.AddMinutes(60);
        var task = new RecurringTask("Task", new[] { new TimeOnly(10, 0) }, urgency: 3);
        var occurrence = new TaskOccurrence(Guid.NewGuid(), task.Id, scheduledAt, terminalStatus);
        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue);
        var opportunity = new NotificationOpportunity(task.Id, occurrence.OccurrenceId, 0, 1.0, currentTime);
        var suppression = NotificationSuppressionResult.Allowed("AllowedNoActiveWork");

        // Act
        var decision = _policy.Evaluate(context, opportunity, suppression);

        // Assert
        Assert.False(decision.ShouldNotify);
        Assert.Equal(0.0, decision.Opacity);
    }

    [Fact]
    public void NotificationEngine_WithPresentationPolicy_ProducesEndToEndDecision()
    {
        // Arrange
        var history = new InMemoryNotificationHistory();
        var timingPolicy = new NotificationTimingPolicy();
        var suppressionPolicy = new NotificationSuppressionPolicy();
        var presentationPolicy = new NotificationPresentationPolicy();
        var engine = new NotificationEngine(timingPolicy, suppressionPolicy, history, presentationPolicy);

        var scheduledAt = new DateTime(2026, 9, 21, 10, 0, 0);
        var task = new RecurringTask("Urgency 6 Task", new[] { new TimeOnly(10, 0) }, urgency: 6);
        var occurrence = new TaskOccurrence(task.Id, scheduledAt);
        var currentTime = scheduledAt.AddMinutes(60); // 100% threshold crossed

        var context = new NotificationContext(task, occurrence, currentTime, SchedulingState.Overdue, skipCount: 4);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldNotify);
        Assert.Equal(NotificationVisualCategory.High, decision.VisualCategory);
        Assert.Equal(0.875, decision.Opacity, precision: 3);
        Assert.Equal(4, decision.SkipCount);
        Assert.Equal(TimeSpan.FromMinutes(60), decision.Elapsed);
        Assert.Equal(TimeSpan.Zero, decision.Remaining);
        Assert.True(history.HasBeenEmitted(occurrence.OccurrenceId, 0));
    }
}
