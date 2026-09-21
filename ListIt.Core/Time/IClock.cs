using System;

namespace ListIt.Core.Time;

/// <summary>
/// Abstraction for providing current time.
/// Allows deterministic time evaluation in tests and decoupled production execution.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time (in UTC).
    /// </summary>
    DateTime Now { get; }
}
