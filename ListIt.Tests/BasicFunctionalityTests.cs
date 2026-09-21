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
}
