using System;
using System.Collections.Generic;
using ListIt.Shell.Windows.Startup;
using Xunit;

namespace ListIt.Tests.Shell.Windows.Startup;

public class WindowsStartupManagerTests
{
    private class FakeStartupStorage : IStartupStorage
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        private static string GetKey(string subKey, string valueName) => $"{subKey}\\{valueName}";

        public string? GetValue(string subKey, string valueName)
        {
            _values.TryGetValue(GetKey(subKey, valueName), out var val);
            return val;
        }

        public void SetValue(string subKey, string valueName, string value)
        {
            _values[GetKey(subKey, valueName)] = value;
        }

        public void DeleteValue(string subKey, string valueName)
        {
            _values.Remove(GetKey(subKey, valueName));
        }
    }

    private readonly FakeStartupStorage _storage = new();
    private readonly string _testExePath = @"C:\Program Files\List-It\ListIt.exe";

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenRegistryValueDoesNotExist()
    {
        var manager = new WindowsStartupManager(_storage, () => _testExePath);
        Assert.False(manager.IsEnabled());
    }

    [Fact]
    public void Enable_SetsRegistryValue_AndSetsIsEnabledTrue()
    {
        var manager = new WindowsStartupManager(_storage, () => _testExePath);

        manager.Enable();

        Assert.True(manager.IsEnabled());
        var stored = _storage.GetValue(WindowsStartupManager.RunSubKey, WindowsStartupManager.DefaultValueName);
        Assert.Equal($"\"{_testExePath}\"", stored);
    }

    [Fact]
    public void Enable_RepeatedCalls_AreIdempotent()
    {
        var manager = new WindowsStartupManager(_storage, () => _testExePath);

        manager.Enable();
        manager.Enable();
        manager.Enable();

        Assert.True(manager.IsEnabled());
        var stored = _storage.GetValue(WindowsStartupManager.RunSubKey, WindowsStartupManager.DefaultValueName);
        Assert.Equal($"\"{_testExePath}\"", stored);
    }

    [Fact]
    public void Disable_RemovesRegistryValue_AndSetsIsEnabledFalse()
    {
        var manager = new WindowsStartupManager(_storage, () => _testExePath);

        manager.Enable();
        Assert.True(manager.IsEnabled());

        manager.Disable();
        Assert.False(manager.IsEnabled());
        Assert.Null(_storage.GetValue(WindowsStartupManager.RunSubKey, WindowsStartupManager.DefaultValueName));
    }

    [Fact]
    public void Disable_RepeatedCalls_AreIdempotent()
    {
        var manager = new WindowsStartupManager(_storage, () => _testExePath);

        manager.Disable();
        manager.Disable();

        Assert.False(manager.IsEnabled());
    }

    [Fact]
    public void Enable_UsesConfiguredExecutablePath()
    {
        var customPath = @"D:\Custom\ListIt.exe";
        var manager = new WindowsStartupManager(_storage, () => customPath);

        manager.Enable();

        Assert.True(manager.IsEnabled());
        var stored = _storage.GetValue(WindowsStartupManager.RunSubKey, WindowsStartupManager.DefaultValueName);
        Assert.Equal($"\"{customPath}\"", stored);
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenRegisteredPathDoesNotMatchCurrentExecutable()
    {
        var manager = new WindowsStartupManager(_storage, () => _testExePath);
        _storage.SetValue(WindowsStartupManager.RunSubKey, WindowsStartupManager.DefaultValueName, @"C:\OtherApp\Other.exe");

        Assert.False(manager.IsEnabled());
    }
}
