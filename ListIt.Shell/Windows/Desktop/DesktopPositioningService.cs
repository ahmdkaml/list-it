using System;
using System.Windows;

namespace ListIt.Shell.Windows.Desktop;

/// <summary>
/// Platform-independent coordinate calculation for lower-right desktop placement.
/// Respects taskbars, work area boundaries, and display margins.
/// </summary>
public static class DesktopPositioningService
{
    public const double DefaultMargin = 16.0;

    /// <summary>
    /// Calculates the top-left coordinate to place a window at the lower-right corner
    /// of the usable work area, clamped to ensure full visibility.
    /// </summary>
    /// <param name="workArea">The usable work area of the target monitor.</param>
    /// <param name="windowSize">The size of the window to position.</param>
    /// <param name="margin">The gap in device-independent pixels between the window and work area edges.</param>
    /// <returns>The calculated Point (Left, Top) in screen coordinates.</returns>
    public static Point CalculateLowerRightPosition(Rect workArea, Size windowSize, double margin = DefaultMargin)
    {
        if (workArea.Width <= 0 || workArea.Height <= 0)
        {
            return new Point(0, 0);
        }

        double width = Math.Max(0, windowSize.Width);
        double height = Math.Max(0, windowSize.Height);

        // Desired position: lower-right corner inset by margin
        double targetX = workArea.Right - width - margin;
        double targetY = workArea.Bottom - height - margin;

        // Clamp so the window never overflows outside the work area boundaries
        double minX = workArea.Left;
        double maxX = Math.Max(workArea.Left, workArea.Right - width);
        double minY = workArea.Top;
        double maxY = Math.Max(workArea.Top, workArea.Bottom - height);

        double clampedX = Math.Clamp(targetX, minX, maxX);
        double clampedY = Math.Clamp(targetY, minY, maxY);

        return new Point(clampedX, clampedY);
    }

    /// <summary>
    /// Calculates the top-left coordinate to center a window within the usable work area.
    /// </summary>
    public static Point CalculateCenterPosition(Rect workArea, Size windowSize)
    {
        if (workArea.Width <= 0 || workArea.Height <= 0)
        {
            return new Point(0, 0);
        }

        double width = Math.Max(0, windowSize.Width);
        double height = Math.Max(0, windowSize.Height);

        double targetX = workArea.Left + (workArea.Width - width) / 2.0;
        double targetY = workArea.Top + (workArea.Height - height) / 2.0;

        return new Point(targetX, targetY);
    }
}
