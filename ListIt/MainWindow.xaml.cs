using System.Windows;
using ListIt.Services;

namespace ListIt;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IShellAnchorService _shellAnchorService;

    public MainWindow() : this(new WindowsShellAnchorService())
    {
    }

    public MainWindow(IShellAnchorService shellAnchorService)
    {
        _shellAnchorService = shellAnchorService;

        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Attach to Windows desktop shell via decoupled service
        _shellAnchorService.AttachToDesktop(this);

        // Center on screen
        Left = (SystemParameters.WorkArea.Width - Width) / 2;
        Top = (SystemParameters.WorkArea.Height - Height) / 2;

        // Remove taskbar icon once window has loaded and rendered
        ShowInTaskbar = false;
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
