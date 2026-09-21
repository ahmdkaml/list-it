using System;
using System.Windows;
using System.Windows.Input;
using ListIt.Services;

namespace ListIt;

/// <summary>
/// Interaction logic for MainWindow.xaml (Shell Window Host).
/// Uses DesktopShellBox for all OS window positioning and desktop anchoring.
/// Outer 8px frame is the draggable grip; inner AppView has normal mouse interaction.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DesktopShellBox _shellBox;

    public MainWindow() : this(new DesktopShellBox())
    {
    }

    public MainWindow(DesktopShellBox shellBox)
    {
        _shellBox = shellBox ?? throw new ArgumentNullException(nameof(shellBox));

        InitializeComponent();
        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _shellBox.Attach(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _shellBox.CenterAndPinToBottom(this);
    }

    private void ShellDragGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            _shellBox.HandleDrag(this);
        }
    }
}
