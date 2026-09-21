using System;
using ListIt.Shell.Notifications;
using Xunit;

namespace ListIt.Tests.Shell.Notifications;

public class NotificationTimeFormatterTests
{
    [Theory]
    [InlineData(0, "0 min")]
    [InlineData(30, "< 1 min")]
    [InlineData(60, "1 min")]
    [InlineData(59 * 60, "59 min")]
    [InlineData(60 * 60, "1h")]
    [InlineData(90 * 60, "1h 30m")]
    public void Format_VariousDurations_ProducesExpectedCompactFormat(int seconds, string expected)
    {
        var result = NotificationTimeFormatter.Format(TimeSpan.FromSeconds(seconds));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_NegativeTime_ReturnsZeroMin()
    {
        var result = NotificationTimeFormatter.Format(TimeSpan.FromSeconds(-10));
        Assert.Equal("0 min", result);
    }
}
