using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            // Get cursor position
            if (!GetCursorPos(out POINT pt)) return null;
            
            // Get window under cursor
            var hwnd = WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero) return null;
            
            // Get the root owner window (for child windows)
            hwnd = GetAncestor(hwnd, GA_ROOT);
            if (hwnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessHelper] Error getting process under cursor: {ex.Message}");
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
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessHelper] Error getting foreground process: {ex.Message}");
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
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return null;

            using var process = Process.GetProcessById((int)processId);
            return process.MainModule?.FileName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessHelper] Error getting foreground process path: {ex.Message}");
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint GA_ROOT = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
}
