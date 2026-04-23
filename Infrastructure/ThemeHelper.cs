using System;
using Microsoft.Win32;

namespace SoftScroll.Infrastructure;

public static class ThemeHelper
{
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
                    return intValue == 0;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[ThemeHelper] Error detecting theme");
        }
        return true;
    }

    public static class Dark
    {
        public const string Background = "#0A0A0B";
        public const string Surface = "#141416";
        public const string SurfaceHover = "#1C1C1F";
        public const string SurfaceActive = "#252529";
        public const string SurfaceBorder = "#2A2A2E";
        public const string Text = "#F4F4F5";
        public const string TextSecondary = "#8B8B94";
        public const string TextTertiary = "#5A5A63";
        public const string Accent = "#3B82F6";
        public const string AccentHover = "#2563EB";
        public const string AccentText = "#FFFFFF";
        public const string Input = "#1C1C1F";
        public const string InputBorder = "#2A2A2E";
        public const string InputFocus = "#3B82F6";
        public const string NavBackground = "#0F0F11";
        public const string NavSelected = "#1C1C1F";
        public const string NavIndicator = "#3B82F6";
    }

    public static class Light
    {
        public const string Background = "#F8F8FA";
        public const string Surface = "#FFFFFF";
        public const string SurfaceHover = "#F4F4F5";
        public const string SurfaceActive = "#ECECEE";
        public const string SurfaceBorder = "#E4E4E7";
        public const string Text = "#18181B";
        public const string TextSecondary = "#71717A";
        public const string TextTertiary = "#A1A1AA";
        public const string Accent = "#3B82F6";
        public const string AccentHover = "#2563EB";
        public const string AccentText = "#FFFFFF";
        public const string Input = "#FFFFFF";
        public const string InputBorder = "#D4D4D8";
        public const string InputFocus = "#3B82F6";
        public const string NavBackground = "#F0F0F2";
        public const string NavSelected = "#FFFFFF";
        public const string NavIndicator = "#3B82F6";
    }
}
