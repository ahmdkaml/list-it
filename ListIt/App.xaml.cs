using System.Windows;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.Core.Time;
using ListIt.Infrastructure.Persistence;
using ListIt.Shell.Notifications;

namespace ListIt;

/// <summary>
/// Interaction logic for App.xaml.
/// Owns the application lifecycle, composition root, and continuous SchedulerRuntime.
/// </summary>
public partial class App : Application
{
    private ISchedulerRuntime? _schedulerRuntime;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Composition root: Repository -> Service -> Scheduling
        var repository = new JsonTaskRepository();
        var taskService = new TaskService(repository);
        var generator = new OccurrenceGenerator();
        var scheduler = new Scheduler();
        var occurrenceService = new OccurrenceService(taskService);
        var schedulingRuntime = new SchedulingRuntime(taskService, generator, scheduler, occurrenceService);

        // Composition root: Notification Engine (Phase 3)
        var notificationTimingPolicy = new NotificationTimingPolicy();
        var notificationSuppressionPolicy = new NotificationSuppressionPolicy();
        var notificationPresentationPolicy = new NotificationPresentationPolicy();
        var notificationHistory = new InMemoryNotificationHistory();
        var notificationEngine = new NotificationEngine(
            notificationTimingPolicy,
            notificationSuppressionPolicy,
            notificationHistory,
            notificationPresentationPolicy);

        // Composition root: Notification Action Handler (Phase 4.3)
        var notificationActionHandler = new NotificationActionHandler(
            schedulingRuntime,
            taskService,
            notificationHistory);

        // Composition root: Windows Notification Presenter (Phase 4.1 & 4.3)
        var notificationPresenter = new WindowsNotificationPresenter(
            actionHandler: notificationActionHandler);

        // Composition root: Continuous Scheduler Runtime (Phase 4.4)
        _schedulerRuntime = new SchedulerRuntime(
            schedulingRuntime,
            notificationEngine,
            notificationPresenter,
            SystemClock.Instance);

        // Shell Window Host
        var mainWindow = new MainWindow(new ListIt.Shell.Windows.Desktop.DesktopShellBox(), taskService, schedulingRuntime);
        MainWindow = mainWindow;
        mainWindow.Show();

        // Start continuous scheduler runtime
        await _schedulerRuntime.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_schedulerRuntime != null)
        {
            await _schedulerRuntime.StopAsync();
        }

        base.OnExit(e);
    }
}
