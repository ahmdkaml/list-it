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
    private TaskItemViewModel? _selectedTask;
    private bool _isEditorOpen;
    private string? _errorMessage;

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = new();
    public TaskEditorViewModel Editor { get; } = new();

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

    public MainViewModel(ITaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));

        OpenCreateTaskCommand = new RelayCommand(OpenCreateTask);
        OpenEditTaskCommand = new RelayCommand(OpenEditTask, () => SelectedTask != null);
        DeleteTaskCommand = new RelayCommand(DeleteSelectedTask, () => SelectedTask != null);
        RefreshTasksCommand = new RelayCommand(LoadTasks);

        Editor.TaskSaved += Editor_TaskSaved;
        Editor.Cancelled += Editor_Cancelled;

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
                    _taskService.CreateRecurringTask(recurring.Title, recurring.AssignedTimes, recurring.Description, recurring.Urgency);
                }
                else if (task is FiniteTask finite)
                {
                    _taskService.CreateFiniteTask(finite.Title, finite.RequiredCompletions, finite.Description, finite.Urgency);
                }
            }

            IsEditorOpen = false;
            LoadTasks();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save task: {ex.Message}";
        }
    }

    private void Editor_Cancelled(object? sender, EventArgs e)
    {
        IsEditorOpen = false;
    }
}
