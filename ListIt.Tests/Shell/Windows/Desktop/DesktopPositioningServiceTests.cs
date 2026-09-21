using System.Windows;
using ListIt.Shell.Windows.Desktop;
using Xunit;

namespace ListIt.Tests.Shell.Windows.Desktop;

public class DesktopPositioningServiceTests
{
    [Fact]
    public void CalculateLowerRightPosition_StandardResolution_ReturnsCorrectCoordinates()
    {
        // Work area: 1920x1040 (bottom 40px taskbar)
        var workArea = new Rect(0, 0, 1920, 1040);
        var windowSize = new Size(320, 440);
        double margin = 16.0;

        var pos = DesktopPositioningService.CalculateLowerRightPosition(workArea, windowSize, margin);

        Assert.Equal(1920 - 320 - 16, pos.X);
        Assert.Equal(1040 - 440 - 16, pos.Y);
    }

    [Fact]
    public void CalculateLowerRightPosition_TopTaskbar_ReturnsCorrectCoordinates()
    {
        // Work area: 0, 40, 1920, 1040 (top 40px taskbar)
        var workArea = new Rect(0, 40, 1920, 1040);
        var windowSize = new Size(320, 440);
        double margin = 16.0;

        var pos = DesktopPositioningService.CalculateLowerRightPosition(workArea, windowSize, margin);

        Assert.Equal(1920 - 320 - 16, pos.X);
        Assert.Equal(40 + 1040 - 440 - 16, pos.Y);
    }

    [Fact]
    public void CalculateLowerRightPosition_LeftTaskbar_ReturnsCorrectCoordinates()
    {
        // Work area: 60, 0, 1860, 1080 (left 60px taskbar)
        var workArea = new Rect(60, 0, 1860, 1080);
        var windowSize = new Size(320, 440);
        double margin = 16.0;

        var pos = DesktopPositioningService.CalculateLowerRightPosition(workArea, windowSize, margin);

        Assert.Equal(60 + 1860 - 320 - 16, pos.X);
        Assert.Equal(1080 - 440 - 16, pos.Y);
    }

    [Fact]
    public void CalculateLowerRightPosition_MultiMonitorOffset_CalculatesWithinTargetMonitor()
    {
        // Secondary monitor: Left = 1920, Top = 0, Width = 1920, Height = 1040
        var workArea = new Rect(1920, 0, 1920, 1040);
        var windowSize = new Size(320, 440);
        double margin = 16.0;

        var pos = DesktopPositioningService.CalculateLowerRightPosition(workArea, windowSize, margin);

        Assert.Equal(1920 + 1920 - 320 - 16, pos.X);
        Assert.Equal(1040 - 440 - 16, pos.Y);
    }

    [Fact]
    public void CalculateLowerRightPosition_WindowLargerThanWorkArea_ClampsToTopLeft()
    {
        var workArea = new Rect(100, 100, 300, 350);
        var windowSize = new Size(400, 500);

        var pos = DesktopPositioningService.CalculateLowerRightPosition(workArea, windowSize, 16.0);

        Assert.Equal(100, pos.X);
        Assert.Equal(100, pos.Y);
    }

    [Fact]
    public void CalculateLowerRightPosition_ZeroOrNegativeWorkArea_ReturnsZeroPoint()
    {
        var workArea = new Rect(0, 0, 0, 0);
        var windowSize = new Size(320, 440);

        var pos = DesktopPositioningService.CalculateLowerRightPosition(workArea, windowSize);

        Assert.Equal(0, pos.X);
        Assert.Equal(0, pos.Y);
    }
}
