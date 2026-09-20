using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ListIt.Services;

/// <summary>
/// Windows OS implementation of IShellAnchorService using Win32 user32.dll P/Invoke.
/// </summary>
public class WindowsShellAnchorService : IShellAnchorService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public bool AttachToDesktop(Window window)
    {
        if (window == null) return false;

        var helper = new WindowInteropHelper(window);
        IntPtr hWnd = helper.Handle;
        if (hWnd == IntPtr.Zero) return false;

        // Note: Calling SetParent(hWnd, progman/workerW) buries WPF layered windows
        // beneath the Windows 10/11 wallpaper rendering layer and breaks WPF message dispatch.
        // For sticker/widget presentation, we keep it as a clean top-level floating window.
        return true;
    }
}
