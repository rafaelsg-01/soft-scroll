using System;
using Serilog;

namespace SoftScroll;

/// <summary>
/// Caches the process name under the cursor with a configurable TTL.
/// Avoids the ~0.1â€“1ms Win32 call chain on every wheel event while
/// still detecting process switches within the TTL window.
/// </summary>
internal static class CachedProcessHelper
{
    private const int TtlMs = 100;

    private static string? _cachedProcess;
    private static long _lastCheckTick;
    private static nint _lastHwnd;

    /// <summary>
    /// Returns the cached process name if still within TTL, otherwise refreshes.
    /// </summary>
    internal static string? GetProcessUnderCursor()
    {
        var now = Environment.TickCount64;
        if (now - _lastCheckTick < TtlMs)
            return _cachedProcess;

        // Resolve the window under cursor to check if it changed
        if (!NativeMethods.GetCursorPos(out var pt))
            return _cachedProcess;

        var hwnd = NativeMethods.WindowFromPoint(pt);
        if (hwnd == IntPtr.Zero)
        {
            _cachedProcess = null;
            _lastCheckTick = now;
            return null;
        }

        hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
        if (hwnd == _lastHwnd)
        {
            // Same window, return cached value even if TTL expired
            return _cachedProcess;
        }

        // Window changed, re-resolve process
        _lastHwnd = hwnd;
        _lastCheckTick = now;

        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0)
            {
                _cachedProcess = null;
                return null;
            }

            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            _cachedProcess = process.ProcessName;
            return _cachedProcess;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[CachedProcessHelper] Error resolving process");
            _cachedProcess = null;
            return null;
        }
    }
}
