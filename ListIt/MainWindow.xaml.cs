using System;
using System.Windows;
using System.Windows.Input;
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
    {
        _shellBox = shellBox ?? throw new ArgumentNullException(nameof(shellBox));

        InitializeComponent();

        // Composition root: Repository -> Service -> Scheduling -> ViewModel
        var repository = new JsonTaskRepository();
        var taskService = new TaskService(repository);
        var generator = new ListIt.Core.Scheduling.OccurrenceGenerator();
        var scheduler = new ListIt.Core.Scheduling.Scheduler();
        var occurrenceService = new ListIt.Core.Scheduling.OccurrenceService(taskService);
        var schedulingRuntime = new ListIt.Core.Scheduling.SchedulingRuntime(taskService, generator, scheduler, occurrenceService);

        // Composition root: Notification Engine (Phase 3)
        var notificationTimingPolicy = new ListIt.Core.Notifications.NotificationTimingPolicy();
        var notificationSuppressionPolicy = new ListIt.Core.Notifications.NotificationSuppressionPolicy();
        var notificationPresentationPolicy = new ListIt.Core.Notifications.NotificationPresentationPolicy();
        var notificationHistory = new ListIt.Core.Notifications.InMemoryNotificationHistory();
        var notificationEngine = new ListIt.Core.Notifications.NotificationEngine(
            notificationTimingPolicy,
            notificationSuppressionPolicy,
            notificationHistory,
            notificationPresentationPolicy);

        // Composition root: Windows Notification Presenter (Phase 4.1)
        var notificationPresenter = new ListIt.Shell.Notifications.WindowsNotificationPresenter();

        var mainViewModel = new MainViewModel(taskService, schedulingRuntime);
        DataContext = mainViewModel;

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Loaded += (s, e) => schedulingRuntime.Start();
        Closed += (s, e) => schedulingRuntime.Dispose();
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
