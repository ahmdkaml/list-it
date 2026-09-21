namespace ListIt.Shell.Windows.Startup;

/// <summary>
/// Abstraction for storing startup registration keys/values (e.g. Windows Registry).
/// Enables deterministic unit testing without touching the actual OS registry.
/// </summary>
public interface IStartupStorage
{
    string? GetValue(string subKey, string valueName);
    void SetValue(string subKey, string valueName, string value);
    void DeleteValue(string subKey, string valueName);
}
