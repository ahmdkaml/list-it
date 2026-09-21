using System;

namespace ListIt.Core.Time;

/// <summary>
/// Production clock implementation returning current UTC system time.
/// </summary>
public class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public DateTime Now => DateTime.UtcNow;
}
