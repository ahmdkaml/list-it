using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Notifications;
using ListIt.Core.Scheduling;
using ListIt.UI.ViewModels.Common;

namespace ListIt.UI.ViewModels;

public class TaskTimingDiagnosticsItemViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<double> DefaultThresholds = HalvingNotificationTimingPolicy.CalculateHalvingThresholds();

    private readonly ListitTask _task;
    private TaskOccurrence? _occurrence;
    private SchedulingState? _schedulingState;
    private bool _isWorking;
    private double _score;

    public TaskTimingDiagnosticsItemViewModel(ListitTask task, TaskOccurrence? occurrence = null)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
        _occurrence = occurrence;
    }

    public Guid TaskId => _task.Id;
    public string Title => _task.Title;
    public string Description => _task.Description;
    public TaskType Type => _task.Type;
    public TimeSpan Interval => _task.Interval;
    public int PassCount => _task.PassCount;
    public int Passes => _task.Passes;
    public bool HasPasses => PassCount > 0;
    public bool HasMultiplePasses => PassCount > 1;
    public string PassesDisplay => $"Passes: {PassCount}";

    public string TypeDisplay => _task.Type switch
    {
        TaskType.Recurring => "Recurring",
        TaskType.Finite => "Finite",
        _ => _task.Type.ToString()
    };

    public string IntervalDisplay => FormatInterval(_task.Interval);

    public TaskOccurrence? Occurrence
    {
        get => _occurrence;
        set
        {
            if (SetProperty(ref _occurrence, value))
            {
                RefreshStaticCalculations();
            }
        }
    }

    public SchedulingState? SchedulingState
    {
        get => _schedulingState;
        set => SetProperty(ref _schedulingState, value);
    }

    public bool IsWorking
    {
        get => _isWorking;
        set => SetProperty(ref _isWorking, value);
    }

    public double Score
    {
        get => _score;
        set
        {
            if (SetProperty(ref _score, value))
            {
                OnPropertyChanged(nameof(ScoreDisplay));
            }
        }
    }

    public string ScoreDisplay => $"Score: {Score:F0}";

    public DateTime IntervalStartUtc
    {
        get
        {
            if (_occurrence != null)
            {
                return _occurrence.ScheduledAt - _task.Interval;
            }
            return _task.StartTime;
        }
    }

    public string IntervalStartDisplay => IntervalStartUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public DateTime DeadlineUtc
    {
        get
        {
            if (_occurrence != null)
            {
                return _occurrence.ScheduledAt;
            }
            return _task.GetNextDeadlineUtc();
        }
    }

    public string DeadlineDisplay => DeadlineUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    // Real-time properties updated via UpdateTick
    private string _deadlineCountdown = "00:00:00";
    public string DeadlineCountdown
    {
        get => _deadlineCountdown;
        set => SetProperty(ref _deadlineCountdown, value);
    }

    private double _progressPercentage;
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }

    private string _progressDisplay = "0.0%";
    public string ProgressDisplay
    {
        get => _progressDisplay;
        set => SetProperty(ref _progressDisplay, value);
    }

    private DateTime? _nextHalvingAlertUtc;
    public DateTime? NextHalvingAlertUtc
    {
        get => _nextHalvingAlertUtc;
        set => SetProperty(ref _nextHalvingAlertUtc, value);
    }

    private double? _nextHalvingThreshold;
    public double? NextHalvingThreshold
    {
        get => _nextHalvingThreshold;
        set => SetProperty(ref _nextHalvingThreshold, value);
    }

    private string _nextHalvingAlertDisplay = "-";
    public string NextHalvingAlertDisplay
    {
        get => _nextHalvingAlertDisplay;
        set => SetProperty(ref _nextHalvingAlertDisplay, value);
    }

    private string _nextHalvingCountdown = "-";
    public string NextHalvingCountdown
    {
        get => _nextHalvingCountdown;
        set => SetProperty(ref _nextHalvingCountdown, value);
    }

    private string _alertStepsSummary = "0 / 5 steps";
    public string AlertStepsSummary
    {
        get => _alertStepsSummary;
        set => SetProperty(ref _alertStepsSummary, value);
    }

    private string _statusDisplay = "Scheduled";
    public string StatusDisplay
    {
        get => _statusDisplay;
        set => SetProperty(ref _statusDisplay, value);
    }

    public void UpdateTick(DateTime utcNow)
    {
        var interval = _task.Interval > TimeSpan.Zero ? _task.Interval : TimeSpan.FromHours(1);
        var windowStart = IntervalStartUtc;
        var deadline = DeadlineUtc;

        // 1. Deadline countdown
        if (utcNow < deadline)
        {
            var rem = deadline - utcNow;
            DeadlineCountdown = FormatTimeSpan(rem);
        }
        else
        {
            var overdue = utcNow - deadline;
            DeadlineCountdown = $"Overdue by {FormatTimeSpan(overdue)}";
        }

        // 2. Interval progress percentage
        var totalSec = interval.TotalSeconds;
        var elapsedSec = (utcNow - windowStart).TotalSeconds;
        var pct = totalSec > 0 ? (elapsedSec / totalSec) * 100.0 : 0.0;
        ProgressPercentage = Math.Clamp(pct, 0.0, 100.0);
        ProgressDisplay = $"{ProgressPercentage:F1}%";

        // 3. Halving alerts calculation
        DateTime? nextAlert = null;
        double? nextThreshold = null;
        int crossedCount = 0;

        foreach (var threshold in DefaultThresholds)
        {
            var alertTime = windowStart.AddSeconds(totalSec * threshold);
            if (utcNow >= alertTime)
            {
                crossedCount++;
            }
            else if (nextAlert == null)
            {
                nextAlert = alertTime;
                nextThreshold = threshold;
            }
        }

        NextHalvingAlertUtc = nextAlert;
        NextHalvingThreshold = nextThreshold;
        AlertStepsSummary = $"{crossedCount} / {DefaultThresholds.Count} halving steps elapsed";

        if (nextAlert.HasValue && nextThreshold.HasValue)
        {
            var alertLocal = nextAlert.Value.ToLocalTime().ToString("HH:mm:ss");
            var thresholdPct = (int)Math.Round(nextThreshold.Value * 100);
            NextHalvingAlertDisplay = $"{alertLocal} ({thresholdPct}% alert)";

            var rem = nextAlert.Value - utcNow;
            NextHalvingCountdown = FormatTimeSpan(rem);
        }
        else if (utcNow < deadline)
        {
            NextHalvingAlertDisplay = "All halving alerts passed (next is deadline)";
            NextHalvingCountdown = DeadlineCountdown;
        }
        else
        {
            NextHalvingAlertDisplay = "Deadline passed";
            NextHalvingCountdown = "Passed";
        }

        // 4. Status display
        if (IsWorking)
        {
            StatusDisplay = "Working";
        }
        else if (utcNow >= deadline)
        {
            StatusDisplay = "Overdue";
        }
        else if (SchedulingState.HasValue)
        {
            StatusDisplay = SchedulingState.Value.ToString();
        }
        else
        {
            StatusDisplay = "Scheduled";
        }
    }

    public void RefreshStaticCalculations()
    {
        OnPropertyChanged(nameof(IntervalStartUtc));
        OnPropertyChanged(nameof(IntervalStartDisplay));
        OnPropertyChanged(nameof(DeadlineUtc));
        OnPropertyChanged(nameof(DeadlineDisplay));
        OnPropertyChanged(nameof(PassCount));
        OnPropertyChanged(nameof(Passes));
        OnPropertyChanged(nameof(HasPasses));
        OnPropertyChanged(nameof(HasMultiplePasses));
        OnPropertyChanged(nameof(PassesDisplay));
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        var totalHours = (int)ts.TotalHours;
        return $"{totalHours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
    }

    private static string FormatInterval(TimeSpan interval)
    {
        if (interval.TotalDays >= 1 && interval.TotalHours % 24 == 0)
        {
            return interval.TotalDays == 1 ? "1 day" : $"{(int)interval.TotalDays} days";
        }
        if (interval.TotalHours >= 1 && interval.TotalMinutes % 60 == 0)
        {
            return interval.TotalHours == 1 ? "1 hour" : $"{(int)interval.TotalHours} hours";
        }
        return $"{(int)interval.TotalMinutes} mins";
    }
}
