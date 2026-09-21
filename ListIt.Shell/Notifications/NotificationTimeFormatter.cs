using System;

namespace ListIt.Shell.Notifications;

/// <summary>
/// UI-level time formatting utility for compact notification display.
/// Pure deterministic formatting free from system clocks.
/// </summary>
public static class NotificationTimeFormatter
{
    public static string Format(TimeSpan time)
    {
        if (time <= TimeSpan.Zero)
        {
            return "0 min";
        }

        if (time < TimeSpan.FromMinutes(1))
        {
            return "< 1 min";
        }

        if (time.TotalHours < 1)
        {
            return $"{time.Minutes} min";
        }

        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;

        return minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
    }
}
