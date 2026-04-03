using System;
using System.Diagnostics;

namespace SoftScroll;

/// <summary>
/// Helper class to get information about the foreground window process.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Gets the process name of the window under the mouse cursor.
    /// This is more reliable than GetForegroundWindow for scroll events.
    /// </summary>
    public static string? GetProcessUnderCursor()
    {
        try
        {
            if (!NativeMethods.GetCursorPos(out var pt)) return null;

            var hwnd = NativeMethods.WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero) return null;

            hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            if (hwnd == IntPtr.Zero) return null;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[ProcessHelper] Error getting process under cursor");
            return null;
        }
    }

    /// <summary>
    /// Gets the process name of the currently focused (foreground) window.
    /// Returns null if unable to determine.
    /// </summary>
    public static string? GetForegroundProcessName()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[ProcessHelper] Error getting foreground process");
            return null;
        }
    }

    /// <summary>
    /// Gets the full path of the currently focused window's process executable.
    /// Returns null if unable to determine.
    /// </summary>
    public static string? GetForegroundProcessPath()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            return process.MainModule?.FileName;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[ProcessHelper] Error getting foreground process path");
            return null;
        }
    }
}
