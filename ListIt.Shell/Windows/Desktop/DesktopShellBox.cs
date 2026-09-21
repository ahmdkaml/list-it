using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ListIt.Shell.Windows.Desktop;

/// <summary>
/// Encapsulates desktop shell management, screen positioning, and window drag handling.
/// Isolates native OS window behavior from UI presentation.
/// </summary>
public class DesktopShellBox
{
    private readonly IShellAnchorService _shellAnchorService;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    public DesktopShellBox() : this(new WindowsShellAnchorService())
    {
    }

    public DesktopShellBox(IShellAnchorService shellAnchorService)
    {
        _shellAnchorService = shellAnchorService ?? throw new ArgumentNullException(nameof(shellAnchorService));
    }

    /// <summary>
    /// Attaches the window to the Windows desktop shell so it remains visible on Win+D
    /// and never elevates over open applications.
    /// </summary>
    public void Attach(Window window)
    {
        if (window == null) return;
        _shellAnchorService.AttachToDesktop(window);
    }

    /// <summary>
    /// Centers the window on the primary display work area and pins it to the bottom of the Z-order.
    /// </summary>
    public void CenterAndPinToBottom(Window window)
    {
        if (window == null) return;

        window.Left = (SystemParameters.WorkArea.Width - window.Width) / 2;
        window.Top = (SystemParameters.WorkArea.Height - window.Height) / 2;

        _shellAnchorService.SendToBottom(window);
    }

    /// <summary>
    /// Positions the window in the lower-right corner of the usable work area and pins it to the bottom of the Z-order.
    /// </summary>
    public void PositionLowerRightAndPin(Window window, double margin = 16.0)
    {
        if (window == null) return;

        var pos = DesktopPositioningService.CalculateLowerRightPosition(
            SystemParameters.WorkArea,
            new Size(window.Width, window.Height),
            margin);

        window.Left = pos.X;
        window.Top = pos.Y;

        _shellAnchorService.SendToBottom(window);
    }

    /// <summary>
    /// Handles window drag-move operations and ensures the window stays pinned to the bottom Z-order upon release.
    /// </summary>
    public void HandleDrag(Window window)
    {
        if (window == null) return;

        try
        {
            window.DragMove();
        }
        catch
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle != IntPtr.Zero)
            {
                ReleaseCapture();
                SendMessage(helper.Handle, 0x0112, 0xF012, 0);
            }
        }
        finally
        {
            _shellAnchorService.SendToBottom(window);
        }
    }
}
