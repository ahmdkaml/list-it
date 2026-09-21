using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.Infrastructure.Persistence;
using ListIt.Shell.Windows.Desktop;
using ListIt.UI.ViewModels;

namespace ListIt;

/// <summary>
/// Interaction logic for MainWindow.xaml (Shell Window Host).
/// Uses DesktopShellBox for all OS window positioning and desktop anchoring.
/// Outer 8px frame is the draggable grip; inner AppView has normal mouse interaction.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DesktopShellBox _shellBox;
    private bool _isExplicitShutdown;

    public MainWindow() : this(new DesktopShellBox())
    {
    }

    public MainWindow(DesktopShellBox shellBox)
        : this(shellBox, CreateDefaultTaskService(out var runtime), runtime)
    {
    }

    public MainWindow(DesktopShellBox shellBox, ITaskService taskService, ISchedulingRuntime schedulingRuntime)
    {
        _shellBox = shellBox ?? throw new ArgumentNullException(nameof(shellBox));
        if (taskService == null) throw new ArgumentNullException(nameof(taskService));
        if (schedulingRuntime == null) throw new ArgumentNullException(nameof(schedulingRuntime));

        InitializeComponent();

        var mainViewModel = new MainViewModel(taskService, schedulingRuntime);
        DataContext = mainViewModel;

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private static ITaskService CreateDefaultTaskService(out ISchedulingRuntime schedulingRuntime)
    {
        var repository = new JsonTaskRepository();
        var taskService = new TaskService(repository);
        var generator = new OccurrenceGenerator();
        var scheduler = new Scheduler();
        var occurrenceService = new OccurrenceService(taskService);
        schedulingRuntime = new SchedulingRuntime(taskService, generator, scheduler, occurrenceService);
        return taskService;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _shellBox.Attach(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _shellBox.PositionLowerRightAndPin(this);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // When user/window close is requested, hide the window rather than destroying it
        // so background SchedulerRuntime continues running
        if (!_isExplicitShutdown)
        {
            e.Cancel = true;
            Hide();
        }
    }

    /// <summary>
    /// Restores the window from hidden/minimized state and activates it in the foreground.
    /// </summary>
    public void RestoreAndActivate()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
    }

    /// <summary>
    /// Explicitly closes the window for application shutdown.
    /// </summary>
    public void ShutdownAndClose()
    {
        _isExplicitShutdown = true;
        Close();
    }

    private void ShellDragGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            _shellBox.HandleDrag(this);
        }
    }
}
