using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftScroll;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EasingMode
{
    ExponentialOut,
    CubicOut,
    QuinticOut,
    Linear
}

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public int StepSizePx { get; set; } = 120;
    public int AnimationTimeMs { get; set; } = 360;
    public int AccelerationDeltaMs { get; set; } = 70;
    public int AccelerationMax { get; set; } = 7;
    public int TailToHeadRatio { get; set; } = 3;

    public bool AnimationEasing { get; set; } = true;
    public EasingMode EasingMode { get; set; } = EasingMode.ExponentialOut;
    public bool ShiftKeyHorizontal { get; set; } = true;
    public bool HorizontalSmoothness { get; set; } = true;
    public bool ReverseWheelDirection { get; set; } = false;

    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = true;

    public string Language { get; set; } = "en";

    public bool ZoomSmoothing { get; set; } = true;
    public bool MomentumEnabled { get; set; } = false;
    public int MomentumFriction { get; set; } = 50;
    public bool MiddleClickScroll { get; set; } = true;
    public int MiddleClickDeadZone { get; set; } = 10;

    public List<string> ExcludedApps { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    public static AppSettings CreatePreset(string presetName) => presetName switch
    {
        "Reading" => new()
        {
            StepSizePx = 80,
            AnimationTimeMs = 500,
            AccelerationDeltaMs = 100,
            AccelerationMax = 3,
            TailToHeadRatio = 5,
            AnimationEasing = true,
            EasingMode = EasingMode.CubicOut
        },
        "Productivity" => new()
        {
            StepSizePx = 160,
            AnimationTimeMs = 250,
            AccelerationDeltaMs = 60,
            AccelerationMax = 10,
            TailToHeadRatio = 2,
            AnimationEasing = true,
            EasingMode = EasingMode.ExponentialOut
        },
        "Gaming" => new()
        {
            StepSizePx = 120,
            AnimationTimeMs = 100,
            AccelerationDeltaMs = 40,
            AccelerationMax = 5,
            TailToHeadRatio = 1,
            AnimationEasing = false,
            EasingMode = EasingMode.Linear
        },
        _ => CreateDefault()
    };

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
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettings] Failed to load settings: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettings] Failed to save settings: {ex.Message}");
        }
    }

    public bool IsExcluded(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        foreach (var app in ExcludedApps)
        {
            if (string.Equals(app, processName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
