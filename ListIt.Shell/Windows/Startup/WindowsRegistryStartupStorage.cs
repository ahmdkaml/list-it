using System;
using Microsoft.Win32;

namespace ListIt.Shell.Windows.Startup;

/// <summary>
/// Production startup storage using the Windows CurrentUser Registry hive.
/// </summary>
public class WindowsRegistryStartupStorage : IStartupStorage
{
    public static readonly WindowsRegistryStartupStorage Instance = new();

    public string? GetValue(string subKey, string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey, writable: false);
            return key?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }

    public void SetValue(string subKey, string valueName, string value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(subKey, writable: true);
            key.SetValue(valueName, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Failed to write registry startup value '{valueName}': {ex}");
        }
    }

    public void DeleteValue(string subKey, string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey, writable: true);
            key?.DeleteValue(valueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Failed to delete registry startup value '{valueName}': {ex}");
        }
    }
}
