using System.Windows;

namespace ListIt.Services;

/// <summary>
/// Service abstraction for anchoring a window into the Windows desktop shell.
/// </summary>
public interface IShellAnchorService
{
    /// <summary>
    /// Anchors the specified WPF window to the Windows desktop shell.
    /// </summary>
    /// <param name="window">The window to attach to the desktop background.</param>
    /// <returns>True if successfully anchored; otherwise, false.</returns>
    bool AttachToDesktop(Window window);
}
