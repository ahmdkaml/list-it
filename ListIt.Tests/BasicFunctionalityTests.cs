using System;
using ListIt.Services;
using Xunit;
using TaskItem = ListIt.Models.Task;

namespace ListIt.Tests;

public class BasicFunctionalityTests
{
    [Fact]
    public void TaskModel_InitializesCorrectly()
    {
        // Arrange & Act
        var task = new TaskItem
        {
            Title = "Test desktop widget task"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Test desktop widget task", task.Title);
    }

    [Fact]
    public void ShellAnchorService_ImplementsInterface()
    {
        // Arrange
        var service = new WindowsShellAnchorService();

        // Assert
        Assert.IsAssignableFrom<IShellAnchorService>(service);
    }

    [Fact]
    public void ShellAnchorService_HandlesNullWindowGracefully()
    {
        // Arrange
        var service = new WindowsShellAnchorService();

        // Act & Assert
        bool attached = service.AttachToDesktop(null!);
        Assert.False(attached);

        // SendToBottom should not throw on null
        var exception = Record.Exception(() => service.SendToBottom(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void DesktopShellBox_HandlesNullWindowGracefully()
    {
        // Arrange
        var box = new DesktopShellBox();

        // Act & Assert
        var attachException = Record.Exception(() => box.Attach(null!));
        Assert.Null(attachException);

        var centerException = Record.Exception(() => box.CenterAndPinToBottom(null!));
        Assert.Null(centerException);

        var dragException = Record.Exception(() => box.HandleDrag(null!));
        Assert.Null(dragException);
    }
}
