using System;
using System.Linq;
using ListIt.Core.Models;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public class TaskItemViewModel : ViewModelBase
{
    private readonly TaskBase _task;

    public TaskItemViewModel(TaskBase task)
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

    public string StateDisplay => SchedulingState.HasValue ? SchedulingState.Value.ToString() : string.Empty;

    public string TypeDisplay => _task.Type switch
    {
        TaskType.Recurring => "Recurring",
        TaskType.Finite => "Finite",
        _ => _task.Type.ToString()
    };

    public string DetailsDisplay => _task switch
    {
        RecurringTask r => r.AssignedTimes.Count > 0
            ? string.Join(", ", r.AssignedTimes.Select(t => t.ToString("HH:mm")))
            : "No schedule",
        FiniteTask f => $"{f.CurrentCompletions} / {f.RequiredCompletions} completed • Due: {FormatDueAt(f.DueAt)}",
        _ => string.Empty
    };

    public TaskBase Task => _task;

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
