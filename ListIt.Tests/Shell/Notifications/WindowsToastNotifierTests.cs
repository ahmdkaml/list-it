using System;
using ListIt.Core.Notifications;
using ListIt.Shell.Notifications;
using Xunit;

namespace ListIt.Tests.Shell.Notifications;

public class WindowsToastNotifierTests
{
    private class FakeActionHandler : INotificationActionHandler
    {
        public NotificationActionContext? LastContext { get; private set; }
        public int CallCount { get; private set; }

        public NotificationActionResult Handle(NotificationActionContext context)
        {
            LastContext = context;
            CallCount++;
            return NotificationActionResult.Success(shouldClosePopup: true);
        }
    }

    private class TestableToastNotifier : WindowsToastNotifier
    {
        public string? LastRemovedTag { get; private set; }
        public string? LastRemovedGroup { get; private set; }
        public bool ClearToastsCalled { get; private set; }

        public TestableToastNotifier(
            INotificationActionHandler? actionHandler = null,
            Action? onActivateApplication = null)
            : base(actionHandler, onActivateApplication)
        {
        }

        public override void RemoveToast(string tag, string? group = null)
        {
            LastRemovedTag = tag;
            LastRemovedGroup = group;
        }

        public override void ClearToasts()
        {
            ClearToastsCalled = true;
        }
    }

    [Fact]
    public void ShowToast_NullRequest_ThrowsArgumentNullException()
    {
        using var notifier = new WindowsToastNotifier();
        Assert.Throws<ArgumentNullException>(() => notifier.ShowToast(null!));
    }

    [Fact]
    public void ShowToast_ValidRequest_DoesNotThrow()
    {
        using var notifier = new WindowsToastNotifier();
        var request = new NotificationPresentationRequest(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 4,
            skipCount: 1,
            elapsed: TimeSpan.FromMinutes(10),
            remaining: TimeSpan.FromMinutes(5),
            visualCategory: NotificationVisualCategory.Medium,
            opacity: 0.8,
            taskTitle: "Test Task");

        // Should not throw even in non-packaged test environment
        var ex = Record.Exception(() => notifier.ShowToast(request));
        Assert.Null(ex);
    }

    [Fact]
    public void ProcessActivationArgument_Open_InvokesOnActivateApplication()
    {
        bool activated = false;
        using var notifier = new WindowsToastNotifier(onActivateApplication: () => activated = true);

        notifier.ProcessActivationArgument("action=open");

        Assert.True(activated);
    }

    [Fact]
    public void ProcessActivationArgument_Work_InvokesActionHandlerWithWork()
    {
        var handler = new FakeActionHandler();
        using var notifier = new TestableToastNotifier(actionHandler: handler);

        var taskId = Guid.NewGuid();
        var occId = Guid.NewGuid();
        var args = $"action=work&taskId={taskId}&occurrenceId={occId}&opportunityIndex=3";

        notifier.ProcessActivationArgument(args);

        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(handler.LastContext);
        Assert.Equal(taskId, handler.LastContext!.TaskId);
        Assert.Equal(occId, handler.LastContext.OccurrenceId);
        Assert.Equal(NotificationAction.Work, handler.LastContext.Action);
        Assert.Equal(3, handler.LastContext.OpportunityIndex);
        Assert.Null(notifier.LastRemovedTag);
    }

    [Fact]
    public void ProcessActivationArgument_Done_InvokesActionHandlerWithDoneAndRemovesToast()
    {
        var handler = new FakeActionHandler();
        using var notifier = new TestableToastNotifier(actionHandler: handler);

        var taskId = Guid.NewGuid();
        var occId = Guid.NewGuid();
        var args = $"action=done&taskId={taskId}&occurrenceId={occId}";

        notifier.ProcessActivationArgument(args);

        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(handler.LastContext);
        Assert.Equal(taskId, handler.LastContext!.TaskId);
        Assert.Equal(occId, handler.LastContext.OccurrenceId);
        Assert.Equal(NotificationAction.Done, handler.LastContext.Action);
        Assert.Equal(occId.ToString(), notifier.LastRemovedTag);
    }

    [Fact]
    public void ProcessActivationArgument_Dismiss_InvokesActionHandlerWithDismissAndRemovesToast()
    {
        var handler = new FakeActionHandler();
        using var notifier = new TestableToastNotifier(actionHandler: handler);

        var taskId = Guid.NewGuid();
        var occId = Guid.NewGuid();
        var args = $"action=dismiss&taskId={taskId}&occurrenceId={occId}";

        notifier.ProcessActivationArgument(args);

        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(handler.LastContext);
        Assert.Equal(NotificationAction.Dismiss, handler.LastContext!.Action);
        Assert.Equal(occId.ToString(), notifier.LastRemovedTag);
    }

    [Fact]
    public void ProcessActivationArgument_EmptyOrMalformed_DoesNotThrow()
    {
        var handler = new FakeActionHandler();
        using var notifier = new WindowsToastNotifier(actionHandler: handler);

        var ex1 = Record.Exception(() => notifier.ProcessActivationArgument(""));
        var ex2 = Record.Exception(() => notifier.ProcessActivationArgument("garbage_data"));
        var ex3 = Record.Exception(() => notifier.ProcessActivationArgument("action=work&taskId=invalid-guid"));

        Assert.Null(ex1);
        Assert.Null(ex2);
        Assert.Null(ex3);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public void ToastActivated_EventFires_WhenProcessActivationArgumentCalled()
    {
        using var notifier = new WindowsToastNotifier();
        string? receivedArg = null;
        notifier.ToastActivated += arg => receivedArg = arg;

        notifier.ProcessActivationArgument("action=open");

        Assert.Equal("action=open", receivedArg);
    }
}
