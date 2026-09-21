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
                OnPropertyChanged(nameof(CanWorkSelectedTask));
                OnPropertyChanged(nameof(CanCompleteSelectedTask));
                OnPropertyChanged(nameof(WorkButtonText));
            }
        }
    }

    public bool HasSelectedTask => SelectedTask != null;
    public bool CanWorkSelectedTask => SelectedTask != null && (SelectedTask.IsWorking || SelectedTask.HasPendingOccurrence);
    public bool CanCompleteSelectedTask => SelectedTask != null && (SelectedTask.HasPendingOccurrence || (_schedulingRuntime == null && SelectedTask.Task is FiniteTask));
    public string WorkButtonText => SelectedTask?.IsWorking == true ? "Stop" : "Work";

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
    public ICommand WorkTaskCommand { get; }
    public ICommand DoneTaskCommand { get; }

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
        OpenEditTaskCommand = new RelayCommand(param => OpenEditTask(param as TaskItemViewModel), param => param is TaskItemViewModel || HasSelectedTask);
        DeleteTaskCommand = new RelayCommand(param => DeleteSelectedTask(param as TaskItemViewModel), param => param is TaskItemViewModel || HasSelectedTask);
        RefreshTasksCommand = new RelayCommand(LoadTasks);

        WorkTaskCommand = new RelayCommand(
            param => WorkTask(param as TaskItemViewModel),
            param => param is TaskItemViewModel item ? (item.IsWorking || item.HasPendingOccurrence) : CanWorkSelectedTask);

        DoneTaskCommand = new RelayCommand(
            param => CompleteTask(param as TaskItemViewModel),
            param => param is TaskItemViewModel item ? (item.HasPendingOccurrence || (_schedulingRuntime == null && item.Task is FiniteTask)) : CanCompleteSelectedTask);

        Editor.TaskSaved += Editor_TaskSaved;
        Editor.Cancelled += Editor_Cancelled;

        if (_schedulingRuntime != null)
        {
            _schedulingRuntime.StateEvaluated += SchedulingRuntime_StateEvaluated;
        }

        LoadTasks();
        SyncEvaluatedStates();
    }

    public void LoadTasks()
    {
        try
        {
            ErrorMessage = null;
            var selectedId = SelectedTask?.Id;
            Tasks.Clear();

            var domainTasks = _taskService.GetAllTasks();
            foreach (var task in domainTasks)
            {
                Tasks.Add(new TaskItemViewModel(task));
            }

            if (selectedId.HasValue)
            {
                SelectedTask = Tasks.FirstOrDefault(t => t.Id == selectedId.Value);
            }
            else
            {
                SelectedTask = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load tasks: {ex.Message}";
        }
    }

    public void WorkTask(TaskItemViewModel? target = null)
    {
        var item = target ?? SelectedTask;
        if (item == null) return;

        try
        {
            ErrorMessage = null;
            if (_schedulingRuntime != null)
            {
                var occ = GetActiveOrPendingOccurrence(item.Id);
                if (occ != null)
                {
                    if (occ.IsWorking)
                    {
                        _schedulingRuntime.StopWorking(occ);
                    }
                    else
                    {
                        _schedulingRuntime.StartWorking(occ);
                    }
                    SyncEvaluatedStates();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update working state: {ex.Message}";
        }
    }

    public void CompleteTask(TaskItemViewModel? target = null)
    {
        var item = target ?? SelectedTask;
        if (item == null) return;

        try
        {
            ErrorMessage = null;
            if (_schedulingRuntime != null)
            {
                var occ = GetActiveOrPendingOccurrence(item.Id);
                if (occ != null)
                {
                    _schedulingRuntime.CompleteOccurrence(occ);
                    LoadTasks();
                    SyncEvaluatedStates();
                    return;
                }
            }

            if (item.Task is FiniteTask finite)
            {
                finite.RecordCompletion();
                if (finite.CurrentCompletions >= finite.RequiredCompletions)
                {
                    _taskService.DeleteTask(finite.Id);
                }
                else
                {
                    _taskService.UpdateTask(finite);
                }
                LoadTasks();
                _schedulingRuntime?.EvaluateNow();
                SyncEvaluatedStates();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to complete task: {ex.Message}";
        }
    }

    private TaskOccurrence? GetActiveOrPendingOccurrence(Guid taskId)
    {
        if (_schedulingRuntime == null) return null;

        var occurrences = _schedulingRuntime.GetOccurrencesForTask(taskId);
        if (occurrences.Count == 0) return null;

        var working = occurrences.FirstOrDefault(o => o.Occurrence.IsWorking);
        if (working != null) return working.Occurrence;

        var pending = occurrences
            .Where(o => o.Occurrence.Status == OccurrenceStatus.Pending)
            .OrderBy(o => o.State switch
            {
                ListIt.Core.Scheduling.SchedulingState.Due => 0,
                ListIt.Core.Scheduling.SchedulingState.Overdue => 1,
                ListIt.Core.Scheduling.SchedulingState.Upcoming => 2,
                _ => 3
            })
            .ThenBy(o => o.Occurrence.ScheduledAt)
            .FirstOrDefault();

        return pending?.Occurrence;
    }

    public void SyncEvaluatedStates(System.Collections.Generic.IReadOnlyList<ListIt.Core.Scheduling.EvaluatedOccurrence>? evaluations = null)
    {
        void Update()
        {
            var evals = evaluations ?? _schedulingRuntime?.CurrentEvaluations;
            if (evals == null)
            {
                foreach (var taskItem in Tasks)
                {
                    taskItem.HasPendingOccurrence = taskItem.Task is FiniteTask f && f.CurrentCompletions < f.RequiredCompletions;
                }
                OnPropertyChanged(nameof(CanWorkSelectedTask));
                OnPropertyChanged(nameof(CanCompleteSelectedTask));
                OnPropertyChanged(nameof(WorkButtonText));
                return;
            }

            var evalMap = evals.GroupBy(e => e.Task.Id).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var taskItem in Tasks)
            {
                if (evalMap.TryGetValue(taskItem.Id, out var taskEvals) && taskEvals.Count > 0)
                {
                    var working = taskEvals.FirstOrDefault(e => e.Occurrence.IsWorking);
                    if (working != null)
                    {
                        taskItem.IsWorking = true;
                        taskItem.SchedulingState = working.State;
                        taskItem.HasPendingOccurrence = true;
                    }
                    else
                    {
                        var primary = taskEvals
                            .Where(e => e.Occurrence.Status == OccurrenceStatus.Pending)
                            .OrderBy(e => e.State switch
                            {
                                ListIt.Core.Scheduling.SchedulingState.Due => 0,
                                ListIt.Core.Scheduling.SchedulingState.Overdue => 1,
                                ListIt.Core.Scheduling.SchedulingState.Upcoming => 2,
                                ListIt.Core.Scheduling.SchedulingState.Completed => 3,
                                _ => 4
                            })
                            .ThenBy(e => e.Occurrence.ScheduledAt)
                            .FirstOrDefault();

                        if (primary != null)
                        {
                            taskItem.IsWorking = false;
                            taskItem.SchedulingState = primary.State;
                            taskItem.HasPendingOccurrence = true;
                        }
                        else
                        {
                            var lastEval = taskEvals.Last();
                            taskItem.IsWorking = false;
                            taskItem.SchedulingState = lastEval.State;
                            taskItem.HasPendingOccurrence = false;
                        }
                    }
                }
                else
                {
                    taskItem.IsWorking = false;
                    taskItem.SchedulingState = null;
                    taskItem.HasPendingOccurrence = false;
                }
            }

            OnPropertyChanged(nameof(CanWorkSelectedTask));
            OnPropertyChanged(nameof(CanCompleteSelectedTask));
            OnPropertyChanged(nameof(WorkButtonText));
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(Update);
        }
        else
        {
            Update();
        }
    }

    private void OpenCreateTask()
    {
        ErrorMessage = null;
        Editor.LoadForCreate();
        IsEditorOpen = true;
    }

    private void OpenEditTask(TaskItemViewModel? target = null)
    {
        var item = target ?? SelectedTask;
        if (item == null) return;

        ErrorMessage = null;
        Editor.LoadForEdit(item.Task);
        IsEditorOpen = true;
    }

    private void DeleteSelectedTask(TaskItemViewModel? target = null)
    {
        var item = target ?? SelectedTask;
        if (item == null) return;

        var taskTitle = item.Title;
        var confirmed = ConfirmDeleteHandler(
            $"Are you sure you want to delete '{taskTitle}'?",
            "Confirm Delete");

        if (!confirmed) return;

        try
        {
            ErrorMessage = null;
            var success = _taskService.DeleteTask(item.Id);
            if (success)
            {
                LoadTasks();
                SelectedTask = null;
                _schedulingRuntime?.EvaluateNow();
                SyncEvaluatedStates();
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
            SyncEvaluatedStates();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save task: {ex.Message}";
        }
    }

    private void SchedulingRuntime_StateEvaluated(object? sender, System.Collections.Generic.IReadOnlyList<ListIt.Core.Scheduling.EvaluatedOccurrence> evaluations)
    {
        SyncEvaluatedStates(evaluations);
    }

    private void Editor_Cancelled(object? sender, EventArgs e)
    {
        IsEditorOpen = false;
    }
}
