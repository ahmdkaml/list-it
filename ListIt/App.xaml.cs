using System.Windows;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.Core.Time;
using ListIt.Infrastructure.Persistence;
using ListIt.Shell.Notifications;
using ListIt.Shell.Options;
using ListIt.Shell.Windows.Desktop;
using ListIt.Shell.Windows.Instance;
using ListIt.Shell.Windows.Startup;

namespace ListIt;

/// <summary>
/// Interaction logic for App.xaml.
/// Owns the application lifecycle, composition root, continuous SchedulerRuntime,
/// and single-instance activation.
/// </summary>
public partial class App : Application
{
    private ISingleInstanceManager? _singleInstanceManager;
    private ISchedulerRuntime? _schedulerRuntime;
    private IStartupManager? _startupManager;
    private ShellOptions _shellOptions = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Single-Instance Check
        _singleInstanceManager = new WindowsSingleInstanceManager();
        if (!_singleInstanceManager.IsFirstInstance)
        {
            _singleInstanceManager.SignalFirstInstance();
            Shutdown(0);
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 2. Shell Configuration & Windows Startup Registration
        _startupManager = new WindowsStartupManager();
        if (_shellOptions.StartWithWindows && !_startupManager.IsEnabled())
        {
            _startupManager.Enable();
        }

        // 3. Composition root: Repository -> Service -> Scheduling
        var repository = new JsonTaskRepository();
        var taskService = new TaskService(repository);
        var generator = new OccurrenceGenerator();
        var scheduler = new Scheduler();
        var occurrenceService = new OccurrenceService(taskService);
        var schedulingRuntime = new SchedulingRuntime(taskService, generator, scheduler, occurrenceService);

        // 4. Composition root: Notification Engine (Phase 3 & Issue #92)
        var notificationTimingPolicy = new HalvingNotificationTimingPolicy();
        var notificationSuppressionPolicy = new NotificationSuppressionPolicy();
        var notificationPresentationPolicy = new NotificationPresentationPolicy();
        var notificationHistory = new InMemoryNotificationHistory();
        var notificationEngine = new NotificationEngine(
            notificationTimingPolicy,
            notificationSuppressionPolicy,
            notificationHistory,
            notificationPresentationPolicy);

        // 5. Composition root: Notification Action Handler (Phase 4.3)
        var notificationActionHandler = new NotificationActionHandler(
            schedulingRuntime,
            taskService,
            notificationHistory);

        // 6. Composition root: Windows Notification Presenter (Phase 4.1 & 4.3)
        var notificationPresenter = new WindowsNotificationPresenter(
            actionHandler: notificationActionHandler);

        // 7. Composition root: Continuous Scheduler Runtime (Phase 4.4)
        _schedulerRuntime = new SchedulerRuntime(
            schedulingRuntime,
            notificationEngine,
            notificationPresenter,
            SystemClock.Instance);

        // 8. Shell Window Host
        var mainWindow = new MainWindow(new DesktopShellBox(), taskService, schedulingRuntime);
        MainWindow = mainWindow;
        mainWindow.Show();

        // 9. Single-Instance Activation Listener
        _singleInstanceManager.StartListeningForActivation(() =>
        {
            Dispatcher.BeginInvoke(mainWindow.RestoreAndActivate);
        });

        // 10. Start continuous scheduler runtime
        await _schedulerRuntime.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_schedulerRuntime != null)
        {
            await _schedulerRuntime.StopAsync();
        }

        _singleInstanceManager?.Dispose();

        if (MainWindow is MainWindow mw)
        {
            mw.ShutdownAndClose();
        }

        base.OnExit(e);
    }
}
