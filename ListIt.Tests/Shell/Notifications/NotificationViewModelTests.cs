using System;
using System.Windows.Media;
using ListIt.Core.Notifications;
using ListIt.Shell.Notifications;
using Xunit;

namespace ListIt.Tests.Shell.Notifications;

public class NotificationViewModelTests
{
    [Fact]
    public void Constructor_PreservesAndFormatsAllProperties()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var request = new NotificationPresentationRequest(
            taskId: taskId,
            occurrenceId: occurrenceId,
            urgency: 4,
            skipCount: 2,
            elapsed: TimeSpan.FromMinutes(25),
            remaining: TimeSpan.FromMinutes(35),
            visualCategory: NotificationVisualCategory.Medium,
            opacity: 0.625,
            taskTitle: "Design Review");

        // Act
        var vm = new NotificationViewModel(request);

        // Assert
        Assert.Equal(taskId, vm.TaskId);
        Assert.Equal(occurrenceId, vm.OccurrenceId);
        Assert.Equal("Design Review", vm.Title);
        Assert.Equal(4, vm.Urgency);
        Assert.Equal("Urgency: 4", vm.UrgencyText);
        Assert.Equal(TimeSpan.FromMinutes(25), vm.Elapsed);
        Assert.Equal("Elapsed: 25 min", vm.ElapsedText);
        Assert.Equal(TimeSpan.FromMinutes(35), vm.Remaining);
        Assert.Equal("Remaining: 35 min", vm.RemainingText);
        Assert.Equal(2, vm.SkipCount);
        Assert.Equal("Skipped: 2", vm.SkipCountText);
        Assert.Equal(NotificationVisualCategory.Medium, vm.VisualCategory);
        Assert.Equal("MEDIUM", vm.CategoryBadgeText);
        Assert.Equal(0.625, vm.Opacity);
    }

    [Theory]
    [InlineData(NotificationVisualCategory.Low, 0x25, 0x63, 0xEB)]     // Blue
    [InlineData(NotificationVisualCategory.Medium, 0x7C, 0x3A, 0xED)]  // Purple
    [InlineData(NotificationVisualCategory.High, 0xDC, 0x26, 0x26)]    // Red
    public void AccentBrush_MapsCorrectColorForCategory(NotificationVisualCategory category, byte r, byte g, byte b)
    {
        // Arrange
        var request = new NotificationPresentationRequest(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 1,
            skipCount: 0,
            elapsed: TimeSpan.Zero,
            remaining: TimeSpan.Zero,
            visualCategory: category,
            opacity: 1.0);

        // Act
        var vm = new NotificationViewModel(request);

        // Assert
        var solidBrush = Assert.IsType<SolidColorBrush>(vm.AccentBrush);
        Assert.Equal(Color.FromRgb(r, g, b), solidBrush.Color);
    }

    [Fact]
    public void Constructor_EmptyTaskTitle_FallsBackToDefaultTitle()
    {
        var request = new NotificationPresentationRequest(
            taskId: Guid.NewGuid(),
            occurrenceId: Guid.NewGuid(),
            urgency: 1,
            skipCount: 0,
            elapsed: TimeSpan.Zero,
            remaining: TimeSpan.Zero,
            visualCategory: NotificationVisualCategory.Low,
            opacity: 1.0,
            taskTitle: "");

        var vm = new NotificationViewModel(request);

        Assert.Equal("Untitled Task", vm.Title);
    }
}
