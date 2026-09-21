using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ListIt.Services;

/// <summary>
/// Windows OS implementation of IShellAnchorService.
/// Attaches the window as an owned window of the desktop shell (SHELLDLL_DefView) using GWLP_HWNDPARENT.
/// This glues the window to the desktop: it stays visible when "Show Desktop" (Win+D) is used,
/// while normal application windows naturally appear above it.
/// </summary>
public class WindowsShellAnchorService : IShellAnchorService
{
    private const int GWLP_HWNDPARENT = -8;

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", EntryPoint = "FindWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        else
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_NOACTIVATE = 0x08000000L;

    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;

    private const int WM_WINDOWPOSCHANGING = 0x0046;

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOZORDER = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public bool AttachToDesktop(Window window)
    {
        if (window == null) return false;

        var helper = new WindowInteropHelper(window);
        IntPtr hWnd = helper.Handle;
        if (hWnd == IntPtr.Zero) return false;

        IntPtr desktopHandle = FindDesktopDefView();
        if (desktopHandle != IntPtr.Zero)
        {
            // Set desktop shell as owner so the window is grouped with desktop layer
            SetWindowLongPtr(hWnd, GWLP_HWNDPARENT, desktopHandle);
        }

        // Set WS_EX_NOACTIVATE so clicking/dragging never activates or raises over other apps
        long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
        exStyle |= WS_EX_NOACTIVATE;
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));

        // Hook window procedure
        var source = HwndSource.FromHwnd(hWnd);
        source?.AddHook(WndProc);

        // Immediately send to bottom of desktop layer (behind any open apps)
        SendToBottom(window);

        return true;
    }

    public void SendToBottom(Window window)
    {
        if (window == null) return;
        var helper = new WindowInteropHelper(window);
        IntPtr hWnd = helper.Handle;
        if (hWnd != IntPtr.Zero)
        {
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_MOUSEACTIVATE:
                // Prevent mouse clicks from activating or bringing window to front
                handled = true;
                return new IntPtr(MA_NOACTIVATE);

            case WM_WINDOWPOSCHANGING:
                if (lParam != IntPtr.Zero)
                {
                    var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    // Only enforce HWND_BOTTOM when a Z-order change is actually requested (SWP_NOZORDER is not set)
                    if ((pos.flags & SWP_NOZORDER) == 0 && pos.hwndInsertAfter != HWND_BOTTOM)
                    {
                        pos.hwndInsertAfter = HWND_BOTTOM;
                        Marshal.StructureToPtr(pos, lParam, true);
                    }
                }
                break;
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindDesktopDefView()
    {
        // 1. Check Shell Window (Progman)
        IntPtr shell = GetShellWindow();
        if (shell != IntPtr.Zero)
        {
            IntPtr defView = FindWindowEx(shell, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero) return defView;
            return shell;
        }

        // 2. Check Progman directly (Windows 11 desktop shell)
        IntPtr progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero) return defView;
            return progman;
        }

        // 3. Check WorkerW windows (Windows 10/11 wallpaper hosts)
        IntPtr workerW = IntPtr.Zero;
        while ((workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null)) != IntPtr.Zero)
        {
            IntPtr defView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero) return defView;
        }

        return IntPtr.Zero;
    }
}
