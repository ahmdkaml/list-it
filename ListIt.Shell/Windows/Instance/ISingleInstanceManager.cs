using System;

namespace ListIt.Shell.Windows.Instance;

/// <summary>
/// Service abstraction for single-instance application detection and inter-process activation.
/// </summary>
public interface ISingleInstanceManager : IDisposable
{
    /// <summary>
    /// Indicates whether the current process is the first (and therefore primary) application instance.
    /// </summary>
    bool IsFirstInstance { get; }

    /// <summary>
    /// Signals the primary application instance from a secondary launch to bring its window to the foreground.
    /// </summary>
    void SignalFirstInstance();

    /// <summary>
    /// Starts listening for activation signals from secondary instances.
    /// </summary>
    /// <param name="onActivated">Callback invoked when an activation signal is received.</param>
    void StartListeningForActivation(Action onActivated);
}
