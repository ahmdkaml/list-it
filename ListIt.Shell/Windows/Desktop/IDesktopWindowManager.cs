using System;
using System.Windows;

namespace ListIt.Shell.Windows.Desktop;

/// <summary>
/// Service abstraction for positioning and anchoring application windows on the desktop.
/// </summary>
public interface IDesktopWindowManager : IDisposable
{
    /// <summary>
    /// Positions the window on the desktop (lower-right region of the work area)
    /// and anchors it to the desktop shell.
    /// </summary>
    void PositionOnDesktop(Window window);

    /// <summary>
    /// Recalculates and updates the window's position based on the current display work area.
    /// </summary>
    void RefreshPosition(Window window);
}
