using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using ListIt.Core.Models;
using ListIt.Core.Scheduling;
using ListIt.Core.Services;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public class DiagnosticsConsoleViewModel : ViewModelBase, IDisposable
{
    private readonly ITaskService _taskService;
    private readonly ISchedulingRuntime? _schedulingRuntime;
    private readonly ITaskScoringService _scoringService;
    private readonly Timer? _timer;
    private bool _isDisposed;

    public ObservableCollection<TaskTimingDiagnosticsItemViewModel> Tasks { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand CloseCommand { get; }

    public event Action? RequestClose;

    public DiagnosticsConsoleViewModel(
        ITaskService taskService,
        ISchedulingRuntime? schedulingRuntime = null,
        ITaskScoringService? scoringService = null,
        bool startTimer = true)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _schedulingRuntime = schedulingRuntime;
        _scoringService = scoringService ?? new TaskScoringService();

        RefreshCommand = new RelayCommand(_ => LoadTasks());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());

        if (_schedulingRuntime != null)
        {
            _schedulingRuntime.StateEvaluated += SchedulingRuntime_StateEvaluated;
        }

        LoadTasks();

        if (startTimer)
        {
            // Update every 1 second
            _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    private void SchedulingRuntime_StateEvaluated(object? sender, System.Collections.Generic.IReadOnlyList<EvaluatedOccurrence> e)
    {
        void Sync()
        {
            LoadTasks();
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(Sync);
        }
        else
        {
            Sync();
        }
    }

    private void OnTimerTick(object? state)
    {
        if (_isDisposed) return;

        void TickAction()
        {
            Tick(DateTime.UtcNow);
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(TickAction);
        }
        else
        {
            TickAction();
        }
    }

    public void Tick(DateTime utcNow)
    {
        foreach (var taskItem in Tasks)
        {
            taskItem.UpdateTick(utcNow);
        }
    }

    public void LoadTasks()
    {
        var domainTasks = _taskService.GetAllTasks();
        var utcNow = DateTime.UtcNow;

        Tasks.Clear();

        foreach (var task in domainTasks)
        {
            TaskOccurrence? primaryOccurrence = null;
            SchedulingState? schedulingState = null;
            bool isWorking = false;

            if (_schedulingRuntime != null)
            {
                var occs = _schedulingRuntime.GetOccurrencesForTask(task.Id);
                var working = occs.FirstOrDefault(o => o.Occurrence.IsWorking);
                if (working != null)
                {
                    primaryOccurrence = working.Occurrence;
                    schedulingState = working.State;
                    isWorking = true;
                }
                else
                {
                    var pending = occs
                        .Where(o => o.Occurrence.Status == OccurrenceStatus.Pending)
                        .OrderBy(o => o.State switch
                        {
                            SchedulingState.Due => 0,
                            SchedulingState.Overdue => 1,
                            SchedulingState.Upcoming => 2,
                            _ => 3
                        })
                        .ThenBy(o => o.Occurrence.ScheduledAt)
                        .FirstOrDefault();

                    if (pending != null)
                    {
                        primaryOccurrence = pending.Occurrence;
                        schedulingState = pending.State;
                    }
                    else if (occs.Count > 0)
                    {
                        primaryOccurrence = occs.Last().Occurrence;
                        schedulingState = occs.Last().State;
                    }
                }
            }

            var itemVm = new TaskTimingDiagnosticsItemViewModel(task, primaryOccurrence)
            {
                SchedulingState = schedulingState,
                IsWorking = isWorking,
                Score = _scoringService.CalculateScore(task, utcNow, isWorking)
            };

            itemVm.UpdateTick(utcNow);
            Tasks.Add(itemVm);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_schedulingRuntime != null)
        {
            _schedulingRuntime.StateEvaluated -= SchedulingRuntime_StateEvaluated;
        }

        _timer?.Dispose();
    }
}
