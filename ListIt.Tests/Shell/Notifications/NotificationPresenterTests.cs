using System;
using System.Collections.Generic;
using ListIt.Core.Notifications;
using ListIt.Shell.Notifications;
using Xunit;

namespace ListIt.Tests.Shell.Notifications;

public class NotificationPresenterTests
{
    [Fact]
    public void Present_ValidDecision_GeneratesPresentationRequest()
    {
        // Arrange
        NotificationPresentationRequest? capturedRequest = null;
        var presenter = new WindowsNotificationPresenter(
            uiDispatcher: action => action(),
            onDisplayRequested: req => capturedRequest = req);

        var taskId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var decision = NotificationDecision.Notify(
            taskId: taskId,
            occurrenceId: occurrenceId,
            urgency: 5,
            skipCount: 2,
            elapsed: TimeSpan.FromMinutes(30),
            remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.High,
            opacity: 0.75);

        // Act
        presenter.Present(decision);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(taskId, capturedRequest!.TaskId);
        Assert.Equal(occurrenceId, capturedRequest.OccurrenceId);
        Assert.Equal(5, capturedRequest.Urgency);
        Assert.Equal(2, capturedRequest.SkipCount);
        Assert.Equal(TimeSpan.FromMinutes(30), capturedRequest.Elapsed);
        Assert.Equal(TimeSpan.Zero, capturedRequest.Remaining);
        Assert.Equal(NotificationVisualCategory.High, capturedRequest.VisualCategory);
        Assert.Equal(0.75, capturedRequest.Opacity);
        Assert.Single(presenter.ActiveRequests);
    }

    [Fact]
    public void Present_NoNotificationDecision_DoesNotGeneratePresentationRequest()
    {
        // Arrange
        bool wasCalled = false;
        var presenter = new WindowsNotificationPresenter(
            uiDispatcher: action => action(),
            onDisplayRequested: _ => wasCalled = true);

        var decision = NotificationDecision.DoNotNotify(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 2);

        // Act
        presenter.Present(decision);

        // Assert
        Assert.False(wasCalled);
        Assert.Empty(presenter.ActiveRequests);
    }

    [Fact]
    public void Present_MetadataPreservation_PreservesAllDecisionFields()
    {
        // Arrange
        NotificationPresentationRequest? captured = null;
        var presenter = new WindowsNotificationPresenter(
            uiDispatcher: action => action(),
            onDisplayRequested: req => captured = req);

        var taskId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var opp = new NotificationOpportunity(taskId, occurrenceId, 1, 0.5, DateTime.UtcNow);

        var decision = new NotificationDecision(
            shouldNotify: true,
            taskId: taskId,
            occurrenceId: occurrenceId,
            urgency: 3,
            skipCount: 4,
            elapsed: TimeSpan.FromHours(1),
            remaining: TimeSpan.FromMinutes(15),
            visualCategory: NotificationVisualCategory.Medium,
            opacity: 0.875,
            suppressionResult: NotificationSuppressionResult.Allowed("AllowedNoActiveWork"),
            opportunities: new[] { opp });

        // Act
        presenter.Present(decision);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(taskId, captured!.TaskId);
        Assert.Equal(occurrenceId, captured.OccurrenceId);
        Assert.Equal(3, captured.Urgency);
        Assert.Equal(4, captured.SkipCount);
        Assert.Equal(TimeSpan.FromHours(1), captured.Elapsed);
        Assert.Equal(TimeSpan.FromMinutes(15), captured.Remaining);
        Assert.Equal(NotificationVisualCategory.Medium, captured.VisualCategory);
        Assert.Equal(0.875, captured.Opacity);
        var capturedOpp = Assert.Single(captured.Opportunities);
        Assert.Equal(opp.LogicalKey, capturedOpp.LogicalKey);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(-10.0, 0.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(5.0, 1.0)]
    public void Present_InvalidOpacity_ClampsDefensivelyBetweenZeroAndOne(double inputOpacity, double expectedOpacity)
    {
        // Arrange
        NotificationPresentationRequest? captured = null;
        var presenter = new WindowsNotificationPresenter(
            uiDispatcher: action => action(),
            onDisplayRequested: req => captured = req);

        var decision = NotificationDecision.Notify(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 1,
            skipCount: 0,
            elapsed: TimeSpan.Zero,
            remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.Low,
            opacity: inputOpacity);

        // Act
        presenter.Present(decision);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(expectedOpacity, captured!.Opacity);
    }

    [Fact]
    public void Present_PresenterFailure_IsolatesErrorAndDoesNotCrash()
    {
        // Arrange
        var presenter = new WindowsNotificationPresenter(
            uiDispatcher: action => action(),
            onDisplayRequested: _ => throw new InvalidOperationException("Simulated Windows UI exception"));

        var decision = NotificationDecision.Notify(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 1,
            skipCount: 0,
            elapsed: TimeSpan.Zero,
            remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.Low,
            opacity: 1.0);

        // Act & Assert - must not throw
        var exception = Record.Exception(() => presenter.Present(decision));
        Assert.Null(exception);
    }

    [Fact]
    public void Present_NullDecision_ThrowsArgumentNullException()
    {
        var presenter = new WindowsNotificationPresenter();
        Assert.Throws<ArgumentNullException>(() => presenter.Present(null!));
    }

    [Fact]
    public void Present_WithTaskTitle_PreservesTitleInRequest()
    {
        NotificationPresentationRequest? captured = null;
        var presenter = new WindowsNotificationPresenter(
            uiDispatcher: action => action(),
            onDisplayRequested: req => captured = req);

        var decision = NotificationDecision.Notify(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 2,
            skipCount: 0,
            elapsed: TimeSpan.Zero,
            remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.Low,
            opacity: 0.5,
            taskTitle: "Important Project Task");

        presenter.Present(decision);

        Assert.NotNull(captured);
        Assert.Equal("Important Project Task", captured!.TaskTitle);
    }
}
