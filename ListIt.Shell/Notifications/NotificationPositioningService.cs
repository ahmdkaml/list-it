using System;
using System.Windows;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Calculates bottom-right desktop positioning and vertical stacking for notification windows.
/// Pure calculation decoupled from WPF window handles for deterministic unit testing.
/// </summary>
public static class NotificationPositioningService
{
    public const double DefaultMargin = 16.0;
    public const double DefaultGap = 8.0;

    /// <summary>
    /// Computes the top-left coordinate for a notification window in the bottom-right corner of the work area.
    /// Stack index 0 is at the very bottom; higher indices stack upward.
    /// </summary>
    public static Point CalculatePosition(
        Rect workArea,
        Size popupSize,
        int stackIndex = 0,
        double margin = DefaultMargin,
        double gap = DefaultGap)
    {
        if (stackIndex < 0) stackIndex = 0;

        double left = workArea.Right - popupSize.Width - margin;
        double top = workArea.Bottom - (popupSize.Height + gap) * (stackIndex + 1) - margin + gap;

        // Defensive clamping to ensure popup remains within the visible work area
        left = Math.Max(workArea.Left + margin, left);
        top = Math.Max(workArea.Top + margin, top);

        return new Point(left, top);
    }
}
