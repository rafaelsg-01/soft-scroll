using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SoftScroll;

/// <summary>
/// Helper class to detect Windows dark/light mode and provide theme colors.
/// </summary>
public static class ThemeHelper
{
    /// <summary>
    /// Returns true if Windows is in dark mode, false if light mode.
    /// </summary>
    public static bool IsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = dark mode, 1 = light mode
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ThemeHelper] Error detecting theme: {ex.Message}");
        }
        return true; // Default to dark mode
    }

    // Dark mode colors (black/white/gray palette)
    public static class Dark
    {
        public const string Background = "#1A1A1A";
        public const string Surface = "#252525";
        public const string SurfaceBorder = "#3A3A3A";
        public const string Text = "#FFFFFF";
        public const string TextSecondary = "#B0B0B0";
        public const string Accent = "#FFFFFF";
        public const string AccentHover = "#E0E0E0";
        public const string Input = "#1E1E1E";
        public const string InputBorder = "#404040";
    }

    // Light mode colors (black/white/gray palette)
    public static class Light
    {
        public const string Background = "#F5F5F5";
        public const string Surface = "#FFFFFF";
        public const string SurfaceBorder = "#D0D0D0";
        public const string Text = "#1A1A1A";
        public const string TextSecondary = "#606060";
        public const string Accent = "#1A1A1A";
        public const string AccentHover = "#404040";
        public const string Input = "#FFFFFF";
        public const string InputBorder = "#C0C0C0";
    }
}
