using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Lightweight desktop notification popup window.
/// Pure presentation component displaying NotificationViewModel data with auto-close lifetime and Escape key handling.
/// </summary>
public partial class NotificationWindow : Window
{
    public const double DefaultLifetimeSeconds = 10.0;
    private readonly DispatcherTimer? _lifetimeTimer;

    public NotificationViewModel ViewModel { get; }

    public NotificationWindow(NotificationViewModel viewModel, double lifetimeSeconds = DefaultLifetimeSeconds)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        DataContext = viewModel;
        Opacity = viewModel.Opacity;

        KeyDown += NotificationWindow_KeyDown;
        Closed += NotificationWindow_Closed;

        if (lifetimeSeconds > 0)
        {
            _lifetimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(lifetimeSeconds)
            };
            _lifetimeTimer.Tick += LifetimeTimer_Tick;
            _lifetimeTimer.Start();
        }
    }

    private void NotificationWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void LifetimeTimer_Tick(object? sender, EventArgs e)
    {
        _lifetimeTimer?.Stop();
        Close();
    }

    private void NotificationWindow_Closed(object? sender, EventArgs e)
    {
        if (_lifetimeTimer != null)
        {
            _lifetimeTimer.Stop();
            _lifetimeTimer.Tick -= LifetimeTimer_Tick;
        }

        KeyDown -= NotificationWindow_KeyDown;
        Closed -= NotificationWindow_Closed;
    }
}
