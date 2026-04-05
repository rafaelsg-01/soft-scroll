using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoftScroll.Infrastructure;

namespace SoftScroll.Settings;

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

    // ── Scroll Settings ─────────────────────────────────────────────
    public int StepSizePx { get; set; } = 120;
    public int AnimationTimeMs { get; set; } = 360;
    public int AccelerationDeltaMs { get; set; } = 70;
    public int AccelerationMax { get; set; } = 7;
    public int TailToHeadRatio { get; set; } = 3;
    public bool AnimationEasing { get; set; } = true;
    public EasingMode EasingMode { get; set; } = EasingMode.ExponentialOut;

    // ── Direction & Horizontal ──────────────────────────────────────
    public bool ShiftKeyHorizontal { get; set; } = true;
    public bool HorizontalSmoothness { get; set; } = true;
    public bool ReverseWheelDirection { get; set; } = false;

    // ── Startup & UI ────────────────────────────────────────────────
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = true;
    public string Language { get; set; } = "en";

    // ── Advanced Features ──────────────────────────────────────────
    public bool ZoomSmoothing { get; set; } = true;
    public bool MomentumEnabled { get; set; } = false;
    public int MomentumFriction { get; set; } = 50;
    public bool MiddleClickScroll { get; set; } = true;
    public int MiddleClickDeadZone { get; set; } = 10;

    // ── App Management ─────────────────────────────────────────────
    public List<string> ExcludedApps { get; set; } = new();
    public List<AppProfile> AppProfiles { get; set; } = new();
    public bool UseAppProfiles { get; set; } = true;

    // ── Quick Toggle ────────────────────────────────────────────────
    public bool EnableGlobalHotkey { get; set; } = true;
    public bool ShowTrayIconState { get; set; } = true;

    // ── Visual Feedback ─────────────────────────────────────────────
    public bool ShowScrollIndicator { get; set; } = false;
    public int ScrollIndicatorDurationMs { get; set; } = 500;
    public IndicatorPosition IndicatorPosition { get; set; } = IndicatorPosition.TopRight;

    // ── Middle-Click Settings ──────────────────────────────────────
    public MiddleClickSettings MiddleClickConfig { get; set; } = new();

    // ── Accessibility ─────────────────────────────────────────────
    public AccessibilitySettings Accessibility { get; set; } = new();

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
                if (s != null)
                {
                    s.Clamp();
                    return s;
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[AppSettings] Failed to load settings");
        }
        return CreateDefault();
    }

    /// <summary>
    /// Clamps all numeric properties to valid ranges. Call after deserialization
    /// to protect against corrupted or hand-edited settings files.
    /// </summary>
    internal void Clamp()
    {
        StepSizePx = Math.Clamp(StepSizePx, 10, 500);
        AnimationTimeMs = Math.Clamp(AnimationTimeMs, 10, 2000);
        AccelerationDeltaMs = Math.Clamp(AccelerationDeltaMs, 0, 500);
        AccelerationMax = Math.Clamp(AccelerationMax, 1, 20);
        TailToHeadRatio = Math.Clamp(TailToHeadRatio, 1, 20);
        MomentumFriction = Math.Clamp(MomentumFriction, 0, 100);
        MiddleClickDeadZone = Math.Clamp(MiddleClickDeadZone, 0, 100);

        if (!LocalizationManager.SupportedLanguages.Contains(Language))
            Language = "en";

        ExcludedApps ??= new List<string>();
        AppProfiles ??= new List<AppProfile>();

        MiddleClickConfig ??= new MiddleClickSettings();
        Accessibility ??= new AccessibilitySettings();
        ScrollIndicatorDurationMs = Math.Clamp(ScrollIndicatorDurationMs, 100, 3000);

        if (MiddleClickConfig.CursorSize < 16) MiddleClickConfig.CursorSize = 16;
        if (MiddleClickConfig.CursorSize > 64) MiddleClickConfig.CursorSize = 64;
        if (MiddleClickConfig.BounceStrength < 0) MiddleClickConfig.BounceStrength = 0;
        if (MiddleClickConfig.BounceStrength > 100) MiddleClickConfig.BounceStrength = 100;

        if (Accessibility.AudioVolume < 0) Accessibility.AudioVolume = 0;
        if (Accessibility.AudioVolume > 1) Accessibility.AudioVolume = 1;
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
            Serilog.Log.Warning(ex, "[AppSettings] Failed to save settings");
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

    public AppProfile? GetAppProfile(string? processName)
    {
        if (!UseAppProfiles || string.IsNullOrEmpty(processName))
            return null;

        foreach (var profile in AppProfiles)
        {
            if (string.Equals(profile.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                return profile;
        }
        return null;
    }
}
