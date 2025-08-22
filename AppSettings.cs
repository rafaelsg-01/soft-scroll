using System;
using System.IO;
using System.Text.Json;

namespace SmoothScrollClone;

public sealed class AppSettings
{
    // Master enable
    public bool Enabled { get; set; } = true;

    // SmoothScroll-like options
    public int StepSizePx { get; set; } = 120;            // Step size [px]
    public int AnimationTimeMs { get; set; } = 360;       // Animation time [ms]
    public int AccelerationDeltaMs { get; set; } = 70;    // Accel window [ms]
    public int AccelerationMax { get; set; } = 7;         // Accel max [x]
    public int TailToHeadRatio { get; set; } = 3;         // Easing power

    public bool AnimationEasing { get; set; } = true;               // Easing on/off
    public bool ShiftKeyHorizontal { get; set; } = true;            // Shift = horizontal
    public bool HorizontalSmoothness { get; set; } = true;          // Smooth horizontal
    public bool ReverseWheelDirection { get; set; } = false;        // Reverse scroll

    public bool StartWithWindows { get; set; } = false;

    public static AppSettings CreateDefault() => new();

    public static string GetConfigPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoftScroll");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            var path = GetConfigPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { }
        return CreateDefault();
    }

    public void Save()
    {
        try
        {
            var path = GetConfigPath();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
