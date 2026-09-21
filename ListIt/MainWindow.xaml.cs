using System;
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
