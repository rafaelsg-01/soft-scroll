using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace SoftScroll;

/// <summary>
/// Manages application startup with Windows via Registry.
/// </summary>
public static class StartupManager
{
    private const string AppName = "SoftScroll";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Sets whether the application should start with Windows.
    /// </summary>
    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
            {
                Debug.WriteLine("[StartupManager] Failed to open Run registry key.");
                return;
            }

            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    Debug.WriteLine($"[StartupManager] Added startup entry: {exePath}");
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Debug.WriteLine("[StartupManager] Removed startup entry.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StartupManager] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the application is configured to start with Windows.
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StartupManager] Error checking startup: {ex.Message}");
            return false;
        }
    }
}
