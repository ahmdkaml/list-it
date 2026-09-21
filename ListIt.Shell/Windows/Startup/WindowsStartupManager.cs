using System;

namespace ListIt.Shell.Windows.Startup;

/// <summary>
/// Manages Windows user-level startup registration under HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Uses the actual running process executable path without requiring administrator privileges.
/// </summary>
public class WindowsStartupManager : IStartupManager
{
    public const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string DefaultValueName = "ListIt";

    private readonly IStartupStorage _storage;
    private readonly Func<string> _executablePathProvider;
    private readonly string _valueName;

    public WindowsStartupManager(
        IStartupStorage? storage = null,
        Func<string>? executablePathProvider = null,
        string valueName = DefaultValueName)
    {
        _storage = storage ?? WindowsRegistryStartupStorage.Instance;
        _executablePathProvider = executablePathProvider ?? (() => Environment.ProcessPath ?? string.Empty);
        _valueName = string.IsNullOrWhiteSpace(valueName) ? DefaultValueName : valueName;
    }

    public bool IsEnabled()
    {
        var existing = _storage.GetValue(RunSubKey, _valueName);
        if (string.IsNullOrWhiteSpace(existing))
        {
            return false;
        }

        var currentPath = _executablePathProvider();
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return false;
        }

        // Check if the registered path matches the current executable (ignoring surrounding quotes)
        var normalizedExisting = existing.Trim('\"').Trim();
        var normalizedCurrent = currentPath.Trim('\"').Trim();

        return string.Equals(normalizedExisting, normalizedCurrent, StringComparison.OrdinalIgnoreCase);
    }

    public void Enable()
    {
        var currentPath = _executablePathProvider();
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return;
        }

        // Quote the path to safely handle spaces in folder paths
        var formattedValue = $"\"{currentPath.Trim('\"')}\"";
        _storage.SetValue(RunSubKey, _valueName, formattedValue);
    }

    public void Disable()
    {
        _storage.DeleteValue(RunSubKey, _valueName);
    }
}
