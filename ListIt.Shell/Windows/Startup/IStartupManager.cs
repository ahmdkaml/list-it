namespace ListIt.Shell.Windows.Startup;

/// <summary>
/// Abstraction for querying and managing Windows user-level startup registration.
/// </summary>
public interface IStartupManager
{
    /// <summary>
    /// Checks whether the application is currently registered to start with Windows.
    /// </summary>
    bool IsEnabled();

    /// <summary>
    /// Registers the application to start with Windows for the current user.
    /// Safe to call repeatedly (idempotent).
    /// </summary>
    void Enable();

    /// <summary>
    /// Removes the application startup registration for the current user.
    /// Safe to call repeatedly (idempotent).
    /// </summary>
    void Disable();
}
