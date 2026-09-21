using System;
using ListIt.Core.Notifications;
using ListIt.Shell.Notifications;
using Xunit;

namespace ListIt.Tests.Shell.Notifications;

public class NotificationViewModelActionTests
{
    private class FakeActionHandler : INotificationActionHandler
    {
        public NotificationActionContext? LastContext { get; private set; }
        public NotificationActionResult ResultToReturn { get; set; } = NotificationActionResult.Success(shouldClosePopup: true);

        public NotificationActionResult Handle(NotificationActionContext context)
        {
            LastContext = context;
            return ResultToReturn;
        }
    }

    [Fact]
    public void WorkCommand_InvokesActionHandler_AndRaisesRequestClose()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occId = Guid.NewGuid();
        var request = new NotificationPresentationRequest(
            taskId, occId, urgency: 2, skipCount: 0,
            elapsed: TimeSpan.Zero, remaining: TimeSpan.FromMinutes(10),
            visualCategory: NotificationVisualCategory.Medium, opacity: 1.0,
            taskTitle: "Test Task");

        var fakeHandler = new FakeActionHandler();
        var vm = new NotificationViewModel(request, fakeHandler);

        bool closeRequested = false;
        vm.RequestClose += (s, e) => closeRequested = true;

        // Act
        Assert.True(vm.WorkCommand.CanExecute(null));
        vm.WorkCommand.Execute(null);

        // Assert
        Assert.NotNull(fakeHandler.LastContext);
        Assert.Equal(taskId, fakeHandler.LastContext.TaskId);
        Assert.Equal(occId, fakeHandler.LastContext.OccurrenceId);
        Assert.Equal(NotificationAction.Work, fakeHandler.LastContext.Action);
        Assert.True(closeRequested);
    }

    [Fact]
    public void DoneCommand_InvokesActionHandler_AndRaisesRequestClose()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occId = Guid.NewGuid();
        var request = new NotificationPresentationRequest(
            taskId, occId, urgency: 3, skipCount: 1,
            elapsed: TimeSpan.FromMinutes(5), remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.High, opacity: 1.0,
            taskTitle: "Urgent Task");

        var fakeHandler = new FakeActionHandler();
        var vm = new NotificationViewModel(request, fakeHandler);

        bool closeRequested = false;
        vm.RequestClose += (s, e) => closeRequested = true;

        // Act
        Assert.True(vm.DoneCommand.CanExecute(null));
        vm.DoneCommand.Execute(null);

        // Assert
        Assert.NotNull(fakeHandler.LastContext);
        Assert.Equal(taskId, fakeHandler.LastContext.TaskId);
        Assert.Equal(occId, fakeHandler.LastContext.OccurrenceId);
        Assert.Equal(NotificationAction.Done, fakeHandler.LastContext.Action);
        Assert.True(closeRequested);
    }

    [Fact]
    public void DismissCommand_InvokesActionHandler_AndRaisesRequestClose()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occId = Guid.NewGuid();
        var opportunity = new NotificationOpportunity(taskId, occId, 1, 0.5, DateTime.UtcNow);
        var request = new NotificationPresentationRequest(
            taskId, occId, urgency: 1, skipCount: 0,
            elapsed: TimeSpan.Zero, remaining: TimeSpan.FromMinutes(20),
            visualCategory: NotificationVisualCategory.Low, opacity: 0.8,
            opportunities: new[] { opportunity },
            taskTitle: "Low Task");

        var fakeHandler = new FakeActionHandler();
        var vm = new NotificationViewModel(request, fakeHandler);

        bool closeRequested = false;
        vm.RequestClose += (s, e) => closeRequested = true;

        // Act
        Assert.True(vm.DismissCommand.CanExecute(null));
        vm.DismissCommand.Execute(null);

        // Assert
        Assert.NotNull(fakeHandler.LastContext);
        Assert.Equal(taskId, fakeHandler.LastContext.TaskId);
        Assert.Equal(occId, fakeHandler.LastContext.OccurrenceId);
        Assert.Equal(NotificationAction.Dismiss, fakeHandler.LastContext.Action);
        Assert.Equal(1, fakeHandler.LastContext.OpportunityIndex);
        Assert.True(closeRequested);
    }

    [Fact]
    public void Commands_WithoutActionHandler_StillRaiseRequestClose()
    {
        // Arrange
        var request = new NotificationPresentationRequest(
            Guid.NewGuid(), Guid.NewGuid(), urgency: 1, skipCount: 0,
            elapsed: TimeSpan.Zero, remaining: TimeSpan.FromMinutes(10),
            visualCategory: NotificationVisualCategory.Low, opacity: 1.0);

        var vm = new NotificationViewModel(request, actionHandler: null);

        bool closeRequested = false;
        vm.RequestClose += (s, e) => closeRequested = true;

        // Act
        vm.DismissCommand.Execute(null);

        // Assert
        Assert.True(closeRequested);
    }

    [Fact]
    public void ActionHandler_ResultWithShouldCloseFalse_DoesNotRaiseRequestClose()
    {
        // Arrange
        var request = new NotificationPresentationRequest(
            Guid.NewGuid(), Guid.NewGuid(), urgency: 1, skipCount: 0,
            elapsed: TimeSpan.Zero, remaining: TimeSpan.FromMinutes(10),
            visualCategory: NotificationVisualCategory.Low, opacity: 1.0);

        var fakeHandler = new FakeActionHandler
        {
            ResultToReturn = NotificationActionResult.Success(shouldClosePopup: false)
        };
        var vm = new NotificationViewModel(request, fakeHandler);

        bool closeRequested = false;
        vm.RequestClose += (s, e) => closeRequested = true;

        // Act
        vm.WorkCommand.Execute(null);

        // Assert
        Assert.False(closeRequested);
    }
}
