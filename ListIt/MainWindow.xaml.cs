using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ListIt.Services;

namespace ListIt;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IShellAnchorService _shellAnchorService;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    public MainWindow() : this(new WindowsShellAnchorService())
    {
    }

    public MainWindow(IShellAnchorService shellAnchorService)
    {
        _shellAnchorService = shellAnchorService;

        InitializeComponent();
        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        // Attach to Windows desktop shell before window is shown so it never flashes over open apps
        _shellAnchorService.AttachToDesktop(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Center on screen
        Left = (SystemParameters.WorkArea.Width - Width) / 2;
        Top = (SystemParameters.WorkArea.Height - Height) / 2;

        _shellAnchorService.SendToBottom(this);
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch
            {
                var helper = new WindowInteropHelper(this);
                ReleaseCapture();
                SendMessage(helper.Handle, 0x0112, 0xF012, 0);
            }
            finally
            {
                _shellAnchorService.SendToBottom(this);
            }
        }
    }
}
