using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ListIt.Core.Models;
using ListIt.Core.Services;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ListIt.Core.Scheduling.ISchedulingRuntime? _schedulingRuntime;
    private TaskItemViewModel? _selectedTask;
    private bool _isEditorOpen;
    private string? _errorMessage;

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = new();
    public TaskEditorViewModel Editor { get; } = new();
    public ListIt.Core.Scheduling.ISchedulingRuntime? SchedulingRuntime => _schedulingRuntime;

    public TaskItemViewModel? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                OnPropertyChanged(nameof(HasSelectedTask));
            }
        }
    }

    public bool HasSelectedTask => SelectedTask != null;

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        set => SetProperty(ref _isEditorOpen, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand OpenCreateTaskCommand { get; }
    public ICommand OpenEditTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand RefreshTasksCommand { get; }

    /// <summary>
    /// Delegate for confirming deletion. Can be overridden in unit tests to avoid MessageBox prompts.
    /// </summary>
    public Func<string, string, bool> ConfirmDeleteHandler { get; set; } =
        (message, title) => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public MainViewModel(ITaskService taskService, ListIt.Core.Scheduling.ISchedulingRuntime? schedulingRuntime = null)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _schedulingRuntime = schedulingRuntime;

        OpenCreateTaskCommand = new RelayCommand(OpenCreateTask);
        OpenEditTaskCommand = new RelayCommand(OpenEditTask, () => SelectedTask != null);
        DeleteTaskCommand = new RelayCommand(DeleteSelectedTask, () => SelectedTask != null);
        RefreshTasksCommand = new RelayCommand(LoadTasks);

        Editor.TaskSaved += Editor_TaskSaved;
        Editor.Cancelled += Editor_Cancelled;

        if (_schedulingRuntime != null)
        {
            _schedulingRuntime.StateEvaluated += SchedulingRuntime_StateEvaluated;
        }

        LoadTasks();
    }

    public void LoadTasks()
    {
        try
        {
            ErrorMessage = null;
            Tasks.Clear();

            var domainTasks = _taskService.GetAllTasks();
            foreach (var task in domainTasks)
            {
                Tasks.Add(new TaskItemViewModel(task));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load tasks: {ex.Message}";
        }
    }

    private void OpenCreateTask()
    {
        ErrorMessage = null;
        Editor.LoadForCreate();
        IsEditorOpen = true;
    }

    private void OpenEditTask()
    {
        if (SelectedTask == null) return;

        ErrorMessage = null;
        Editor.LoadForEdit(SelectedTask.Task);
        IsEditorOpen = true;
    }

    private void DeleteSelectedTask()
    {
        if (SelectedTask == null) return;

        var taskTitle = SelectedTask.Title;
        var confirmed = ConfirmDeleteHandler(
            $"Are you sure you want to delete '{taskTitle}'?",
            "Confirm Delete");

        if (!confirmed) return;

        try
        {
            ErrorMessage = null;
            var success = _taskService.DeleteTask(SelectedTask.Id);
            if (success)
            {
                LoadTasks();
                SelectedTask = null;
                _schedulingRuntime?.EvaluateNow();
            }
            else
            {
                ErrorMessage = "Task could not be deleted.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete task: {ex.Message}";
        }
    }

    private void Editor_TaskSaved(object? sender, TaskBase task)
    {
        try
        {
            ErrorMessage = null;

            if (Editor.IsEditing)
            {
                _taskService.UpdateTask(task);
            }
            else
            {
                if (task is RecurringTask recurring)
                {
                    _taskService.CreateRecurringTask(recurring.Title, recurring.AssignedTimes, recurring.Description, recurring.Urgency, recurring.BypassPrioritySuppression);
                }
                else if (task is FiniteTask finite)
                {
                    _taskService.CreateFiniteTask(finite.Title, finite.RequiredCompletions, finite.Description, finite.Urgency, finite.DueAt, finite.BypassPrioritySuppression);
                }
            }

            IsEditorOpen = false;
            LoadTasks();
            _schedulingRuntime?.EvaluateNow();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save task: {ex.Message}";
        }
    }

    private void SchedulingRuntime_StateEvaluated(object? sender, System.Collections.Generic.IReadOnlyList<ListIt.Core.Scheduling.EvaluatedOccurrence> evaluations)
    {
        void UpdateStates()
        {
            var evalMap = evaluations.GroupBy(e => e.Task.Id).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var taskItem in Tasks)
            {
                if (evalMap.TryGetValue(taskItem.Id, out var evals) && evals.Count > 0)
                {
                    var primary = evals.OrderBy(e => e.State switch
                    {
                        ListIt.Core.Scheduling.SchedulingState.Due => 0,
                        ListIt.Core.Scheduling.SchedulingState.Overdue => 1,
                        ListIt.Core.Scheduling.SchedulingState.Upcoming => 2,
                        ListIt.Core.Scheduling.SchedulingState.Completed => 3,
                        _ => 4
                    }).First();

                    taskItem.SchedulingState = primary.State;
                }
            }
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(UpdateStates);
        }
        else
        {
            UpdateStates();
        }
    }

    private void Editor_Cancelled(object? sender, EventArgs e)
    {
        IsEditorOpen = false;
    }
}
