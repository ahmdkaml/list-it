using System;
using ListIt.Shell.Windows.Desktop;
using Xunit;

namespace ListIt.Tests.Shell;

public class DesktopShellBoxTests
{
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
