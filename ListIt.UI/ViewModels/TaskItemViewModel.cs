using System;
using ListIt.Core.Models;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public class TaskItemViewModel : ViewModelBase
{
    private readonly ListitTask _task;

    public TaskItemViewModel(ListitTask task)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
    }

    public Guid Id => _task.Id;
    public string Title => _task.Title;
    public string Description => _task.Description;
    public int Urgency => _task.Urgency;
    public bool BypassPrioritySuppression => _task.BypassPrioritySuppression;
    public TaskType Type => _task.Type;

    private ListIt.Core.Scheduling.SchedulingState? _schedulingState;
    public ListIt.Core.Scheduling.SchedulingState? SchedulingState
    {
        get => _schedulingState;
        set
        {
            if (SetProperty(ref _schedulingState, value))
            {
                OnPropertyChanged(nameof(StateDisplay));
            }
        }
    }

    private bool _isWorking;
    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            if (SetProperty(ref _isWorking, value))
            {
                OnPropertyChanged(nameof(StateDisplay));
                OnPropertyChanged(nameof(WorkActionText));
            }
        }
    }

    private bool _hasPendingOccurrence;
    public bool HasPendingOccurrence
    {
        get => _hasPendingOccurrence;
        set => SetProperty(ref _hasPendingOccurrence, value);
    }

    public string WorkActionText => IsWorking ? "Stop" : "Work";

    public string StateDisplay
    {
        get
        {
            if (IsWorking) return "Working";
            return SchedulingState.HasValue ? SchedulingState.Value.ToString() : string.Empty;
        }
    }

    public string TypeDisplay => _task.Type switch
    {
        TaskType.Recurring => "Recurring",
        TaskType.Finite => "Finite",
        _ => _task.Type.ToString()
    };

    public string DetailsDisplay
    {
        get
        {
            var intervalStr = FormatInterval(_task.Interval);
            var passesStr = _task.Passes > 0 ? $" • Passes: {_task.Passes}" : string.Empty;

            if (_task.Type == TaskType.Finite)
            {
                var deadlineStr = FormatDueAt(_task.GetNextDeadlineUtc());
                return $"{_task.CurrentCompletions} / {_task.RequiredCompletions} completed • Due: {deadlineStr}{passesStr}";
            }

            return $"Every {intervalStr}{passesStr}";
        }
    }

    public ListitTask Task => _task;

    private static string FormatInterval(TimeSpan interval)
    {
        if (interval.TotalDays >= 1 && interval.TotalHours % 24 == 0)
        {
            return interval.TotalDays == 1 ? "1 day" : $"{(int)interval.TotalDays} days";
        }
        if (interval.TotalHours >= 1 && interval.TotalMinutes % 60 == 0)
        {
            return interval.TotalHours == 1 ? "1 hour" : $"{(int)interval.TotalHours} hours";
        }
        return $"{(int)interval.TotalMinutes} mins";
    }

    private static string FormatDueAt(DateTime utcDueAt)
    {
        var localDue = utcDueAt.ToLocalTime();
        var today = DateTime.Today;

        if (localDue.Date == today)
        {
            return $"Today {localDue:HH:mm}";
        }

        if (localDue.Date == today.AddDays(1))
        {
            return $"Tomorrow {localDue:HH:mm}";
        }

        return localDue.ToString("MMM d, HH:mm");
    }
}
