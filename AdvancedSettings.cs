using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftScroll;

public class AccessibilitySettings
{
    public bool EnableScreenReaderAnnouncements { get; set; } = false;
    public bool AnnounceScrollPosition { get; set; } = true;
    public bool AnnounceScrollSpeed { get; set; } = false;
    public bool HighContrastMode { get; set; } = false;
    public bool EnableAudioFeedback { get; set; } = false;
    public float AudioVolume { get; set; } = 0.5f;
    public SoundType ScrollSound { get; set; } = SoundType.SoftTick;
}

public enum SoundType
{
    SoftTick,
    Click,
    Custom
}

public enum CursorStyle
{
    Arrow,
    Hand,
    Custom
}

public class MiddleClickSettings
{
    public bool ShowCursor { get; set; } = true;
    public CursorStyle CursorStyle { get; set; } = CursorStyle.Arrow;
    public int CursorSize { get; set; } = 32;
    public bool InvertScrollDirection { get; set; } = false;
    public bool EnableEdgeBounce { get; set; } = true;
    public int BounceStrength { get; set; } = 20;
}

public class AppProfile
{
    public string AppName { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public int StepSizePx { get; set; } = 100;
    public int AnimationTimeMs { get; set; } = 150;
    public EasingMode EasingMode { get; set; } = EasingMode.ExponentialOut;
    public int AccelerationDeltaMs { get; set; } = 70;
    public int AccelerationMax { get; set; } = 7;
    public int TailToHeadRatio { get; set; } = 3;
    public bool AnimationEasing { get; set; } = true;
    public bool MomentumEnabled { get; set; } = false;
    public int MomentumFriction { get; set; } = 50;
    public bool Enabled { get; set; } = true;

    public AppSettings ToAppSettings()
    {
        return new AppSettings
        {
            StepSizePx = StepSizePx,
            AnimationTimeMs = AnimationTimeMs,
            EasingMode = EasingMode,
            AccelerationDeltaMs = AccelerationDeltaMs,
            AccelerationMax = AccelerationMax,
            TailToHeadRatio = TailToHeadRatio,
            AnimationEasing = AnimationEasing,
            MomentumEnabled = MomentumEnabled,
            MomentumFriction = MomentumFriction
        };
    }

    public static AppProfile FromAppSettings(string appName, string processName, AppSettings settings)
    {
        return new AppProfile
        {
            AppName = appName,
            ProcessName = processName,
            StepSizePx = settings.StepSizePx,
            AnimationTimeMs = settings.AnimationTimeMs,
            EasingMode = settings.EasingMode,
            AccelerationDeltaMs = settings.AccelerationDeltaMs,
            AccelerationMax = settings.AccelerationMax,
            TailToHeadRatio = settings.TailToHeadRatio,
            AnimationEasing = settings.AnimationEasing,
            MomentumEnabled = settings.MomentumEnabled,
            MomentumFriction = settings.MomentumFriction
        };
    }
}

public enum IndicatorPosition
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
    Center
}

public class ScrollStatistics
{
    private static readonly ScrollStatistics _instance = new();
    public static ScrollStatistics Instance => _instance;

    private long _totalScrollEvents;
    private long _totalPixelsScrolled;
    private long _sessionScrollEvents;
    private long _sessionPixelsScrolled;
    private DateTime _sessionStart = DateTime.Now;
    private readonly object _lock = new();

    public long TotalScrollEvents => _totalScrollEvents;
    public long TotalPixelsScrolled => _totalPixelsScrolled;
    public long SessionScrollEvents => _sessionScrollEvents;
    public long SessionPixelsScrolled => _sessionPixelsScrolled;
    public TimeSpan ActiveTime => DateTime.Now - _sessionStart;
    public DateTime SessionStart => _sessionStart;

    public string FormattedActiveTime
    {
        get
        {
            var ts = ActiveTime;
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }

    public string FormattedTotalPixels
    {
        get
        {
            if (_totalPixelsScrolled >= 1_000_000)
                return $"{_totalPixelsScrolled / 1_000_000.0:F1}M px";
            if (_totalPixelsScrolled >= 1_000)
                return $"{_totalPixelsScrolled / 1_000.0:F1}K px";
            return $"{_totalPixelsScrolled} px";
        }
    }

    public string FormattedSessionPixels
    {
        get
        {
            if (_sessionPixelsScrolled >= 1_000_000)
                return $"{_sessionPixelsScrolled / 1_000_000.0:F1}M px";
            if (_sessionPixelsScrolled >= 1_000)
                return $"{_sessionPixelsScrolled / 1_000.0:F1}K px";
            return $"{_sessionPixelsScrolled} px";
        }
    }

    public void RecordScroll(int pixels)
    {
        lock (_lock)
        {
            System.Threading.Interlocked.Increment(ref _totalScrollEvents);
            System.Threading.Interlocked.Increment(ref _sessionScrollEvents);
            System.Threading.Interlocked.Add(ref _totalPixelsScrolled, Math.Abs(pixels));
            System.Threading.Interlocked.Add(ref _sessionPixelsScrolled, Math.Abs(pixels));
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _totalScrollEvents = 0;
            _totalPixelsScrolled = 0;
            _sessionScrollEvents = 0;
            _sessionPixelsScrolled = 0;
            _sessionStart = DateTime.Now;
        }
    }

    public void ResetSession()
    {
        lock (_lock)
        {
            _sessionScrollEvents = 0;
            _sessionPixelsScrolled = 0;
            _sessionStart = DateTime.Now;
        }
    }
}

public static class PresetManager
{
    public static AppSettings FromPreset(string presetName)
    {
        return presetName switch
        {
            "Reading" => new AppSettings
            {
                StepSizePx = 80,
                AnimationTimeMs = 500,
                AccelerationDeltaMs = 100,
                AccelerationMax = 3,
                TailToHeadRatio = 5,
                AnimationEasing = true,
                EasingMode = EasingMode.CubicOut,
                MomentumEnabled = true,
                MomentumFriction = 70
            },
            "Productivity" => new AppSettings
            {
                StepSizePx = 160,
                AnimationTimeMs = 250,
                AccelerationDeltaMs = 60,
                AccelerationMax = 10,
                TailToHeadRatio = 2,
                AnimationEasing = true,
                EasingMode = EasingMode.ExponentialOut,
                MomentumEnabled = true,
                MomentumFriction = 40
            },
            "Gaming" => new AppSettings
            {
                StepSizePx = 300,
                AnimationTimeMs = 50,
                AccelerationDeltaMs = 40,
                AccelerationMax = 5,
                TailToHeadRatio = 1,
                AnimationEasing = false,
                EasingMode = EasingMode.Linear,
                MomentumEnabled = false,
                MomentumFriction = 50
            },
            "Speed" => new AppSettings
            {
                StepSizePx = 200,
                AnimationTimeMs = 80,
                AccelerationDeltaMs = 50,
                AccelerationMax = 8,
                TailToHeadRatio = 1,
                AnimationEasing = true,
                EasingMode = EasingMode.Linear,
                MomentumEnabled = true,
                MomentumFriction = 30
            },
            "Precise" => new AppSettings
            {
                StepSizePx = 30,
                AnimationTimeMs = 100,
                AccelerationDeltaMs = 80,
                AccelerationMax = 2,
                TailToHeadRatio = 2,
                AnimationEasing = true,
                EasingMode = EasingMode.Linear,
                MomentumEnabled = true,
                MomentumFriction = 80
            },
            _ => AppSettings.CreateDefault()
        };
    }

    public static string[] GetPresetNames() => new[] { "Default", "Reading", "Productivity", "Gaming", "Speed", "Precise" };
}
