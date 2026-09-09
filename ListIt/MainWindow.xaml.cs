using System.Collections.ObjectModel;
using System.Windows;

namespace ListIt;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Models.Task> _tasks = [];

    public MainWindow()
    {
        InitializeComponent();

        TaskList.ItemsSource = _tasks;
    }

    private void AddTask_Click(object sender, RoutedEventArgs e)
    {
        var title = TaskInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return;

        _tasks.Add(new Models.Task
        {
            Title = title
        });

        TaskInput.Clear();
        TaskInput.Focus();
    }

    private void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.DataContext is Models.Task task)
        {
            _tasks.Remove(task);
        }
    }
}
