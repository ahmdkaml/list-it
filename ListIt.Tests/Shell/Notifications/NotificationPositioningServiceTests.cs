using System.Windows;
using ListIt.Shell.Notifications;
using Xunit;

namespace ListIt.Tests.Shell.Notifications;

public class NotificationPositioningServiceTests
{
    [Fact]
    public void CalculatePosition_StackIndex0_PositionsInBottomRightCorner()
    {
        // Arrange
        var workArea = new Rect(0, 0, 1920, 1080);
        var popupSize = new Size(320, 160);

        // Act
        var point = NotificationPositioningService.CalculatePosition(workArea, popupSize, stackIndex: 0, margin: 16, gap: 8);

        // Assert
        // Left = 1920 - 320 - 16 = 1584
        // Top = 1080 - 160 - 16 = 904
        Assert.Equal(1584, point.X);
        Assert.Equal(904, point.Y);
    }

    [Fact]
    public void CalculatePosition_StackIndex1_StacksDirectlyAboveIndex0()
    {
        // Arrange
        var workArea = new Rect(0, 0, 1920, 1080);
        var popupSize = new Size(320, 160);

        // Act
        var point0 = NotificationPositioningService.CalculatePosition(workArea, popupSize, stackIndex: 0, margin: 16, gap: 8);
        var point1 = NotificationPositioningService.CalculatePosition(workArea, popupSize, stackIndex: 1, margin: 16, gap: 8);

        // Assert
        Assert.Equal(point0.X, point1.X);
        // Distance between top of point1 and top of point0 should be popupSize.Height + gap = 168
        Assert.Equal(168, point0.Y - point1.Y);
    }

    [Fact]
    public void CalculatePosition_NegativeStackIndex_TreatedAsZero()
    {
        var workArea = new Rect(0, 0, 1920, 1080);
        var popupSize = new Size(320, 160);

        var point = NotificationPositioningService.CalculatePosition(workArea, popupSize, stackIndex: -5);
        var point0 = NotificationPositioningService.CalculatePosition(workArea, popupSize, stackIndex: 0);

        Assert.Equal(point0.X, point.X);
        Assert.Equal(point0.Y, point.Y);
    }

    [Fact]
    public void CalculatePosition_HighStackIndex_ClampsToTopOfWorkArea()
    {
        var workArea = new Rect(0, 0, 1920, 1080);
        var popupSize = new Size(320, 160);

        // Stack index 20 would normally push Top far into negative coordinates
        var point = NotificationPositioningService.CalculatePosition(workArea, popupSize, stackIndex: 20, margin: 16, gap: 8);

        // Clamped to workArea.Top + margin = 0 + 16 = 16
        Assert.Equal(16, point.Y);
    }
}
