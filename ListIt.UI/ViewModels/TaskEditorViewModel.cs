using System;
using System.Windows.Input;
using ListIt.Core.Models;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public enum IntervalPreset
{
    Day,
    Week,
    Month,
    Custom
}

public enum StartTimeMode
{
    Now,
    SpecificHour
}

public class TaskEditorViewModel : ViewModelBase
{
    private bool _isEditing;
    private Guid? _existingTaskId;
    private DateTime _existingStartTime;
    private int _existingCurrentCompletions;
    private int _existingPasses;
    private TaskType _selectedType;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private int _urgency = 1;
    private string _startTimeString = string.Empty;
    private string _intervalString = "1d";
    private IntervalPreset _selectedIntervalPreset = IntervalPreset.Day;
    private StartTimeMode _selectedStartTimeMode = StartTimeMode.Now;
    private int _requiredCompletions = 1;
    private bool _bypassPrioritySuppression;
    private string? _errorMessage;

    public bool BypassPrioritySuppression
    {
        get => _bypassPrioritySuppression;
        set => SetProperty(ref _bypassPrioritySuppression, value);
    }

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

    public string StartTimeString
    {
        get => _startTimeString;
        set => SetProperty(ref _startTimeString, value);
    }

    public string IntervalString
    {
        get => _intervalString;
        set => SetProperty(ref _intervalString, value);
    }

    public IntervalPreset SelectedIntervalPreset
    {
        get => _selectedIntervalPreset;
        set
        {
            if (SetProperty(ref _selectedIntervalPreset, value))
            {
                OnPropertyChanged(nameof(IsCustomInterval));
                switch (value)
                {
                    case IntervalPreset.Day:
                        IntervalString = "1d";
                        break;
                    case IntervalPreset.Week:
                        IntervalString = "1w";
                        break;
                    case IntervalPreset.Month:
                        IntervalString = "30d";
                        break;
                }
            }
        }
    }

    public bool IsCustomInterval => SelectedIntervalPreset == IntervalPreset.Custom;

    public StartTimeMode SelectedStartTimeMode
    {
        get => _selectedStartTimeMode;
        set
        {
            if (SetProperty(ref _selectedStartTimeMode, value))
            {
                OnPropertyChanged(nameof(IsSpecificStartTime));
                if (value == StartTimeMode.Now)
                {
                    StartTimeString = "Now";
                }
                else if (string.IsNullOrWhiteSpace(StartTimeString) || StartTimeString.Equals("now", StringComparison.OrdinalIgnoreCase))
                {
                    StartTimeString = DateTime.Now.ToString("HH:mm");
                }
            }
        }
    }

    public bool IsSpecificStartTime => SelectedStartTimeMode == StartTimeMode.SpecificHour;

    public int RequiredCompletions
    {
        get => _requiredCompletions;
        set => SetProperty(ref _requiredCompletions, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<ListitTask>? TaskSaved;
    public event EventHandler? Cancelled;

    public TaskEditorViewModel()
    {
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Cancelled?.Invoke(this, EventArgs.Empty));

        LoadForCreate();
    }

    public void LoadForCreate()
    {
        IsEditing = false;
        _existingTaskId = null;
        _existingStartTime = DateTime.UtcNow;
        _existingCurrentCompletions = 0;
        _existingPasses = 0;
        SelectedType = TaskType.Recurring;
        Title = string.Empty;
        Description = string.Empty;
        Urgency = 1;
        SelectedIntervalPreset = IntervalPreset.Day;
        IntervalString = "1d";
        SelectedStartTimeMode = StartTimeMode.Now;
        StartTimeString = "Now";
        RequiredCompletions = 1;
        BypassPrioritySuppression = false;
        ErrorMessage = null;
    }

    public void LoadForEdit(ListitTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        IsEditing = true;
        _existingTaskId = task.Id;
        _existingStartTime = task.StartTime;
        _existingCurrentCompletions = task.CurrentCompletions;
        _existingPasses = task.Passes;
        SelectedType = task.Type;
        Title = task.Title;
        Description = task.Description;
        Urgency = task.Urgency;
        BypassPrioritySuppression = task.BypassPrioritySuppression;

        if (task.Interval == TimeSpan.FromDays(1))
        {
            SelectedIntervalPreset = IntervalPreset.Day;
        }
        else if (task.Interval == TimeSpan.FromDays(7))
        {
            SelectedIntervalPreset = IntervalPreset.Week;
        }
        else if (task.Interval == TimeSpan.FromDays(30))
        {
            SelectedIntervalPreset = IntervalPreset.Month;
        }
        else
        {
            SelectedIntervalPreset = IntervalPreset.Custom;
        }

        IntervalString = FormatInterval(task.Interval);
        SelectedStartTimeMode = StartTimeMode.SpecificHour;
        StartTimeString = task.StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        RequiredCompletions = task.RequiredCompletions;
        ErrorMessage = null;
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

        if (!TryParseInterval(IntervalString, out var interval))
        {
            ErrorMessage = "Invalid interval (e.g. '1d', '4h', '30m').";
            return;
        }

        if (!TryParseStartTime(StartTimeString, out var startTimeUtc))
        {
            ErrorMessage = "Invalid start time (e.g. '09:00', 'yyyy-MM-dd HH:mm', or leave blank for now).";
            return;
        }

        if (SelectedType == TaskType.Finite && RequiredCompletions < 1)
        {
            ErrorMessage = "Required completions must be at least 1.";
            return;
        }

        if (IsEditing && SelectedType == TaskType.Finite && RequiredCompletions < _existingCurrentCompletions)
        {
            ErrorMessage = $"Required completions cannot be less than current ({_existingCurrentCompletions}).";
            return;
        }

        try
        {
            ListitTask resultTask;
            if (IsEditing && ExistingTaskId.HasValue)
            {
                resultTask = new ListitTask(
                    id: ExistingTaskId.Value,
                    title: Title,
                    type: SelectedType,
                    interval: interval,
                    description: Description,
                    urgency: Urgency,
                    startTime: startTimeUtc,
                    requiredCompletions: RequiredCompletions,
                    currentCompletions: _existingCurrentCompletions,
                    passes: _existingPasses,
                    bypassPrioritySuppression: BypassPrioritySuppression);
            }
            else
            {
                resultTask = new ListitTask(
                    title: Title,
                    type: SelectedType,
                    interval: interval,
                    description: Description,
                    urgency: Urgency,
                    startTime: startTimeUtc,
                    requiredCompletions: RequiredCompletions,
                    bypassPrioritySuppression: BypassPrioritySuppression);
            }

            TaskSaved?.Invoke(this, resultTask);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public static bool TryParseInterval(string? input, out TimeSpan interval)
    {
        interval = TimeSpan.FromDays(1);
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var trimmed = input.Trim().ToLowerInvariant();

        if (TimeSpan.TryParse(trimmed, out interval) && interval > TimeSpan.Zero)
        {
            return true;
        }

        if (trimmed.EndsWith("weeks") || trimmed.EndsWith("week") || trimmed.EndsWith("w"))
        {
            var numStr = trimmed.Replace("weeks", "").Replace("week", "").Replace("w", "").Trim();
            if (double.TryParse(numStr, out var weeks) && weeks > 0)
            {
                interval = TimeSpan.FromDays(weeks * 7);
                return true;
            }
        }
        else if (trimmed.EndsWith("months") || trimmed.EndsWith("month"))
        {
            var numStr = trimmed.Replace("months", "").Replace("month", "").Trim();
            if (double.TryParse(numStr, out var months) && months > 0)
            {
                interval = TimeSpan.FromDays(months * 30);
                return true;
            }
        }
        else if (trimmed.EndsWith("days") || trimmed.EndsWith("day") || trimmed.EndsWith("d"))
        {
            var numStr = trimmed.TrimEnd('s').TrimEnd('y').TrimEnd('a').TrimEnd('d').Trim();
            if (double.TryParse(numStr, out var days) && days > 0)
            {
                interval = TimeSpan.FromDays(days);
                return true;
            }
        }
        else if (trimmed.EndsWith("hours") || trimmed.EndsWith("hour") || trimmed.EndsWith("h"))
        {
            var numStr = trimmed.TrimEnd('s').TrimEnd('r').TrimEnd('u').TrimEnd('o').TrimEnd('h').Trim();
            if (double.TryParse(numStr, out var hours) && hours > 0)
            {
                interval = TimeSpan.FromHours(hours);
                return true;
            }
        }
        else if (trimmed.EndsWith("mins") || trimmed.EndsWith("min") || trimmed.EndsWith("m"))
        {
            var numStr = trimmed.TrimEnd('s').TrimEnd('n').TrimEnd('i').TrimEnd('m').Trim();
            if (double.TryParse(numStr, out var mins) && mins > 0)
            {
                interval = TimeSpan.FromMinutes(mins);
                return true;
            }
        }
        else if (double.TryParse(trimmed, out var num) && num > 0)
        {
            interval = TimeSpan.FromDays(num);
            return true;
        }

        return false;
    }

    public static bool TryParseStartTime(string? input, out DateTime startTimeUtc)
    {
        startTimeUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(input) || input.Trim().Equals("now", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var trimmed = input.Trim();
        if (TimeOnly.TryParse(trimmed, out var timeOnly))
        {
            var today = DateTime.Today.Add(timeOnly.ToTimeSpan());
            startTimeUtc = DateTime.SpecifyKind(today, DateTimeKind.Local).ToUniversalTime();
            return true;
        }

        if (DateTime.TryParse(trimmed, out var dt))
        {
            startTimeUtc = dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
                : dt.ToUniversalTime();
            return true;
        }

        return false;
    }

    private static string FormatInterval(TimeSpan interval)
    {
        if (interval.TotalDays == 7)
        {
            return "1w";
        }
        if (interval.TotalDays == 30)
        {
            return "30d";
        }
        if (interval.TotalDays >= 1 && interval.TotalHours % 24 == 0)
        {
            return $"{(int)interval.TotalDays}d";
        }
        if (interval.TotalHours >= 1 && interval.TotalMinutes % 60 == 0)
        {
            return $"{(int)interval.TotalHours}h";
        }
        return $"{(int)interval.TotalMinutes}m";
    }
}
