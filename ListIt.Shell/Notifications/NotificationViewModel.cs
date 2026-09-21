using System;
using System.Windows.Media;
using ListIt.Core.Notifications;

namespace ListIt.Shell.Notifications;

/// <summary>
/// Presentation ViewModel for NotificationWindow.
/// Formats structured NotificationPresentationRequest data for WPF XAML binding.
/// </summary>
public class NotificationViewModel
{
    public Guid TaskId { get; }
    public Guid OccurrenceId { get; }
    public string Title { get; }
    public int Urgency { get; }
    public string UrgencyText => $"Urgency: {Urgency}";
    public TimeSpan Elapsed { get; }
    public string ElapsedText => $"Elapsed: {NotificationTimeFormatter.Format(Elapsed)}";
    public TimeSpan Remaining { get; }
    public string RemainingText => $"Remaining: {NotificationTimeFormatter.Format(Remaining)}";
    public int SkipCount { get; }
    public string SkipCountText => $"Skipped: {SkipCount}";
    public NotificationVisualCategory VisualCategory { get; }
    public string CategoryBadgeText => VisualCategory.ToString().ToUpperInvariant();
    public double Opacity { get; }

    public Brush AccentBrush { get; }
    public Brush BackgroundBrush { get; }
    public Brush CardBorderBrush { get; }
    public Brush TextPrimaryBrush { get; }
    public Brush TextSecondaryBrush { get; }
    public Brush ButtonBackgroundBrush { get; }

    public NotificationViewModel(NotificationPresentationRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        TaskId = request.TaskId;
        OccurrenceId = request.OccurrenceId;
        Title = string.IsNullOrWhiteSpace(request.TaskTitle) ? "Untitled Task" : request.TaskTitle;
        Urgency = request.Urgency;
        Elapsed = request.Elapsed;
        Remaining = request.Remaining;
        SkipCount = request.SkipCount;
        VisualCategory = request.VisualCategory;
        Opacity = request.Opacity;

        AccentBrush = GetAccentBrush(request.VisualCategory);
        BackgroundBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));       // Dark card background
        CardBorderBrush = new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44));       // Subtle border
        TextPrimaryBrush = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));      // High contrast text
        TextSecondaryBrush = new SolidColorBrush(Color.FromRgb(0xA6, 0xAD, 0xC8));    // Muted text
        ButtonBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44)); // Button surface

        AccentBrush.Freeze();
        BackgroundBrush.Freeze();
        CardBorderBrush.Freeze();
        TextPrimaryBrush.Freeze();
        TextSecondaryBrush.Freeze();
        ButtonBackgroundBrush.Freeze();
    }

    private static SolidColorBrush GetAccentBrush(NotificationVisualCategory category)
    {
        return category switch
        {
            NotificationVisualCategory.Low => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),     // Blue
            NotificationVisualCategory.Medium => new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),  // Purple
            NotificationVisualCategory.High => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),    // Red
            _ => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB))
        };
    }
}
