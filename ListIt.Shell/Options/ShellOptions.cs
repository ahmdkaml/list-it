namespace ListIt.Shell.Options;

/// <summary>
/// Configuration options for Windows desktop shell integration.
/// </summary>
public sealed class ShellOptions
{
    /// <summary>
    /// Whether the application should be registered to start automatically with Windows.
    /// Default: true.
    /// </summary>
    public bool StartWithWindows { get; init; } = true;

    /// <summary>
    /// Whether the desktop widget should appear in the Windows taskbar.
    /// Default: false (desktop utility behavior).
    /// </summary>
    public bool ShowInTaskbar { get; init; } = false;

    /// <summary>
    /// Whether the window should be anchored to the desktop background layer.
    /// Default: true.
    /// </summary>
    public bool AnchorToDesktop { get; init; } = true;

    /// <summary>
    /// Margin in device-independent pixels between the window and the work area edge.
    /// Default: 16.0.
    /// </summary>
    public double DesktopMargin { get; init; } = 16.0;
}
