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
    public TaskType Type => _task.Type;

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
        FiniteTask f => $"{f.CurrentCompletions} / {f.RequiredCompletions} completed",
        _ => string.Empty
    };

    public TaskBase Task => _task;
}
