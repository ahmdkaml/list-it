using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ListIt.Core.Models;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public class TaskEditorViewModel : ViewModelBase
{
    private bool _isEditing;
    private Guid? _existingTaskId;
    private DateTime _existingCreatedAt;
    private int _existingCurrentCompletions;
    private TaskType _selectedType;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private int _urgency = 1;
    private string _newTimeString = "09:00";
    private int _requiredCompletions = 1;
    private string _finiteDueTimeString = string.Empty;
    private string? _errorMessage;

    public ObservableCollection<TimeOnly> AssignedTimes { get; } = new();

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsTypeSelectionEnabled));
                OnPropertyChanged(nameof(EditorTitle));
            }
        }
    }

    public Guid? ExistingTaskId => _existingTaskId;

    public bool IsTypeSelectionEnabled => !IsEditing;

    public string EditorTitle => IsEditing ? "Edit Task" : "New Task";

    public TaskType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                OnPropertyChanged(nameof(IsRecurring));
                OnPropertyChanged(nameof(IsFinite));
            }
        }
    }

    public bool IsRecurring => SelectedType == TaskType.Recurring;
    public bool IsFinite => SelectedType == TaskType.Finite;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int Urgency
    {
        get => _urgency;
        set => SetProperty(ref _urgency, value);
    }

    public string NewTimeString
    {
        get => _newTimeString;
        set => SetProperty(ref _newTimeString, value);
    }

    public int RequiredCompletions
    {
        get => _requiredCompletions;
        set => SetProperty(ref _requiredCompletions, value);
    }

    public string FiniteDueTimeString
    {
        get => _finiteDueTimeString;
        set => SetProperty(ref _finiteDueTimeString, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand AddTimeCommand { get; }
    public ICommand RemoveTimeCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<TaskBase>? TaskSaved;
    public event EventHandler? Cancelled;

    public TaskEditorViewModel()
    {
        AddTimeCommand = new RelayCommand(AddTime);
        RemoveTimeCommand = new RelayCommand(RemoveTime);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Cancelled?.Invoke(this, EventArgs.Empty));

        LoadForCreate();
    }

    public void LoadForCreate()
    {
        IsEditing = false;
        _existingTaskId = null;
        _existingCreatedAt = DateTime.UtcNow;
        _existingCurrentCompletions = 0;
        SelectedType = TaskType.Recurring;
        Title = string.Empty;
        Description = string.Empty;
        Urgency = 1;
        NewTimeString = "09:00";
        RequiredCompletions = 1;
        FiniteDueTimeString = string.Empty;
        ErrorMessage = null;

        AssignedTimes.Clear();
        AssignedTimes.Add(new TimeOnly(9, 0));
    }

    public void LoadForEdit(TaskBase task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        IsEditing = true;
        _existingTaskId = task.Id;
        _existingCreatedAt = task.CreatedAt;
        SelectedType = task.Type;
        Title = task.Title;
        Description = task.Description;
        Urgency = task.Urgency;
        ErrorMessage = null;

        AssignedTimes.Clear();
        if (task is RecurringTask recurring)
        {
            foreach (var time in recurring.AssignedTimes)
            {
                AssignedTimes.Add(time);
            }
            _existingCurrentCompletions = 0;
            RequiredCompletions = 1;
            FiniteDueTimeString = string.Empty;
        }
        else if (task is FiniteTask finite)
        {
            _existingCurrentCompletions = finite.CurrentCompletions;
            RequiredCompletions = finite.RequiredCompletions;
            FiniteDueTimeString = finite.DueAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
    }

    private void AddTime()
    {
        if (TimeOnly.TryParse(NewTimeString, out var parsed))
        {
            if (!AssignedTimes.Contains(parsed))
            {
                AssignedTimes.Add(parsed);
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Time already added.";
            }
        }
        else
        {
            ErrorMessage = "Invalid time format (use HH:mm).";
        }
    }

    private void RemoveTime(object? parameter)
    {
        if (parameter is TimeOnly time)
        {
            AssignedTimes.Remove(time);
        }
    }

    private void Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title cannot be empty.";
            return;
        }

        if (Urgency is < 1 or > 6)
        {
            ErrorMessage = "Urgency must be between 1 and 6.";
            return;
        }

        try
        {
            TaskBase resultTask;

            if (SelectedType == TaskType.Recurring)
            {
                if (AssignedTimes.Count == 0)
                {
                    ErrorMessage = "Recurring task must have at least one assigned time.";
                    return;
                }

                if (IsEditing && ExistingTaskId.HasValue)
                {
                    resultTask = new RecurringTask(ExistingTaskId.Value, Title, Description, Urgency, _existingCreatedAt, AssignedTimes);
                }
                else
                {
                    resultTask = new RecurringTask(Title, AssignedTimes, Description, Urgency);
                }
            }
            else
            {
                if (RequiredCompletions < 1)
                {
                    ErrorMessage = "Required completions must be at least 1.";
                    return;
                }

                DateTime? dueAt = null;
                if (!string.IsNullOrWhiteSpace(FiniteDueTimeString))
                {
                    if (TimeOnly.TryParse(FiniteDueTimeString, out var timeOnly))
                    {
                        var todayTime = DateTime.Today.Add(timeOnly.ToTimeSpan());
                        var localTime = todayTime <= DateTime.Now ? todayTime.AddDays(1) : todayTime;
                        dueAt = DateTime.SpecifyKind(localTime, DateTimeKind.Local).ToUniversalTime();
                    }
                    else if (DateTime.TryParse(FiniteDueTimeString, out var dateTime))
                    {
                        dueAt = dateTime.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
                            : dateTime.ToUniversalTime();
                    }
                    else
                    {
                        ErrorMessage = "Invalid due time (e.g. '18:00' or 'yyyy-MM-dd HH:mm', or leave blank for 1 day).";
                        return;
                    }
                }

                if (IsEditing && ExistingTaskId.HasValue)
                {
                    if (RequiredCompletions < _existingCurrentCompletions)
                    {
                        ErrorMessage = $"Required completions cannot be less than current ({_existingCurrentCompletions}).";
                        return;
                    }

                    resultTask = new FiniteTask(ExistingTaskId.Value, Title, Description, Urgency, _existingCreatedAt, RequiredCompletions, _existingCurrentCompletions, dueAt);
                }
                else
                {
                    resultTask = new FiniteTask(Title, RequiredCompletions, currentCompletions: 0, description: Description, urgency: Urgency, dueAt: dueAt);
                }
            }

            TaskSaved?.Invoke(this, resultTask);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
