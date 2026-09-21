using System;
using System.Windows;
using ListIt.Shell.Options;
using Microsoft.Win32;

namespace ListIt.Shell.Windows.Desktop;

/// <summary>
/// Windows-specific desktop window manager.
/// Positions windows in the lower-right usable work area, anchors them to the desktop shell,
/// and responds to display resolution and multi-monitor changes.
/// </summary>
public class WindowsDesktopWindowManager : IDesktopWindowManager
{
    private readonly IShellAnchorService _shellAnchorService;
    private readonly ShellOptions _options;
    private WeakReference<Window>? _trackedWindow;
    private bool _isDisposed;

    public WindowsDesktopWindowManager(
        IShellAnchorService? shellAnchorService = null,
        ShellOptions? options = null)
    {
        _shellAnchorService = shellAnchorService ?? new WindowsShellAnchorService();
        _options = options ?? new ShellOptions();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public void PositionOnDesktop(Window window)
    {
        if (window == null) return;

        _trackedWindow = new WeakReference<Window>(window);

        // Calculate and apply lower-right coordinates
        ApplyPosition(window);

        // Anchor to desktop shell if enabled
        if (_options.AnchorToDesktop)
        {
            _shellAnchorService.AttachToDesktop(window);
            _shellAnchorService.SendToBottom(window);
        }
    }

    public void RefreshPosition(Window window)
    {
        if (window == null) return;

        ApplyPosition(window);

        if (_options.AnchorToDesktop)
        {
            _shellAnchorService.SendToBottom(window);
        }
    }

    private void ApplyPosition(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        double width = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        double height = window.ActualHeight > 0 ? window.ActualHeight : window.Height;

        var pos = DesktopPositioningService.CalculateLowerRightPosition(
            workArea,
            new Size(width, height),
            _options.DesktopMargin);

        window.Left = pos.X;
        window.Top = pos.Y;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_trackedWindow != null && _trackedWindow.TryGetTarget(out var window))
        {
            if (window.Dispatcher.CheckAccess())
            {
                RefreshPosition(window);
            }
            else
            {
                window.Dispatcher.BeginInvoke(() => RefreshPosition(window));
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
