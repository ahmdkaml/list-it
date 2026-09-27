using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ListIt.UI.ViewModels;

namespace ListIt;

public partial class DiagnosticsConsoleWindow : Window
{
    private readonly DiagnosticsConsoleViewModel _viewModel;
    private bool _isDisposed;

    public DiagnosticsConsoleViewModel ViewModel => _viewModel;

    public DiagnosticsConsoleWindow(DiagnosticsConsoleViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.RequestClose += OnRequestClose;
        Closing += DiagnosticsConsoleWindow_Closing;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnRequestClose()
    {
        Hide();
    }

    private void DiagnosticsConsoleWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isDisposed)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ShowOrActivate()
    {
        _viewModel.LoadTasks();
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
    }

    public void DisposeWindow()
    {
        _isDisposed = true;
        _viewModel.RequestClose -= OnRequestClose;
        _viewModel.Dispose();
        Close();
    }
}
