using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoftScroll;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public SettingsViewModel(AppSettings settings)
    {
        ExcludedApps = new ObservableCollection<string>();
        Apply(settings);
    }

    public void Apply(AppSettings s)
    {
        Enabled = s.Enabled;
        StepSizePx = s.StepSizePx;
        AnimationTimeMs = s.AnimationTimeMs;
        AccelerationDeltaMs = s.AccelerationDeltaMs;
        AccelerationMax = s.AccelerationMax;
        TailToHeadRatio = s.TailToHeadRatio;
        AnimationEasing = s.AnimationEasing;
        EasingMode = s.EasingMode;
        ShiftKeyHorizontal = s.ShiftKeyHorizontal;
        HorizontalSmoothness = s.HorizontalSmoothness;
        ReverseWheelDirection = s.ReverseWheelDirection;
        StartWithWindows = s.StartWithWindows;
        StartMinimized = s.StartMinimized;
        ZoomSmoothing = s.ZoomSmoothing;
        MomentumEnabled = s.MomentumEnabled;
        MomentumFriction = s.MomentumFriction;
        MiddleClickScroll = s.MiddleClickScroll;
        MiddleClickDeadZone = s.MiddleClickDeadZone;
        Language = s.Language;

        ExcludedApps.Clear();
        foreach (var app in s.ExcludedApps)
            ExcludedApps.Add(app);
    }

    public AppSettings Snapshot() => new()
    {
        Enabled = Enabled,
        StepSizePx = StepSizePx,
        AnimationTimeMs = AnimationTimeMs,
        AccelerationDeltaMs = AccelerationDeltaMs,
        AccelerationMax = AccelerationMax,
        TailToHeadRatio = TailToHeadRatio,
        AnimationEasing = AnimationEasing,
        EasingMode = EasingMode,
        ShiftKeyHorizontal = ShiftKeyHorizontal,
        HorizontalSmoothness = HorizontalSmoothness,
        ReverseWheelDirection = ReverseWheelDirection,
        StartWithWindows = StartWithWindows,
        StartMinimized = StartMinimized,
        Language = Language,
        ZoomSmoothing = ZoomSmoothing,
        MomentumEnabled = MomentumEnabled,
        MomentumFriction = MomentumFriction,
        MiddleClickScroll = MiddleClickScroll,
        MiddleClickDeadZone = MiddleClickDeadZone,
        ExcludedApps = new List<string>(ExcludedApps)
    };

    public void ApplyPreset(string presetName)
    {
        var preset = AppSettings.CreatePreset(presetName);
        StepSizePx = preset.StepSizePx;
        AnimationTimeMs = preset.AnimationTimeMs;
        AccelerationDeltaMs = preset.AccelerationDeltaMs;
        AccelerationMax = preset.AccelerationMax;
        TailToHeadRatio = preset.TailToHeadRatio;
        AnimationEasing = preset.AnimationEasing;
        EasingMode = preset.EasingMode;
    }

    private bool _enabled;
    public bool Enabled { get => _enabled; set { if (Set(ref _enabled, value)) OnSettingsChanged(); } }

    private int _stepSizePx;
    public int StepSizePx { get => _stepSizePx; set { if (Set(ref _stepSizePx, value)) OnSettingsChanged(); } }

    private int _animationTimeMs;
    public int AnimationTimeMs { get => _animationTimeMs; set { if (Set(ref _animationTimeMs, value)) OnSettingsChanged(); } }

    private int _accelDeltaMs;
    public int AccelerationDeltaMs { get => _accelDeltaMs; set { if (Set(ref _accelDeltaMs, value)) OnSettingsChanged(); } }

    private int _accelMax;
    public int AccelerationMax { get => _accelMax; set { if (Set(ref _accelMax, value)) OnSettingsChanged(); } }

    private int _tailToHead;
    public int TailToHeadRatio { get => _tailToHead; set { if (Set(ref _tailToHead, value)) OnSettingsChanged(); } }

    private bool _easing;
    public bool AnimationEasing { get => _easing; set { if (Set(ref _easing, value)) OnSettingsChanged(); } }

    private EasingMode _easingMode;
    public EasingMode EasingMode { get => _easingMode; set { if (Set(ref _easingMode, value)) OnSettingsChanged(); } }

    private bool _shiftHorizontal;
    public bool ShiftKeyHorizontal { get => _shiftHorizontal; set { if (Set(ref _shiftHorizontal, value)) OnSettingsChanged(); } }

    private bool _horizontalSmooth;
    public bool HorizontalSmoothness { get => _horizontalSmooth; set { if (Set(ref _horizontalSmooth, value)) OnSettingsChanged(); } }

    private bool _reverse;
    public bool ReverseWheelDirection { get => _reverse; set { if (Set(ref _reverse, value)) OnSettingsChanged(); } }

    private bool _startWithWindows;
    public bool StartWithWindows { get => _startWithWindows; set { if (Set(ref _startWithWindows, value)) OnSettingsChanged(); } }

    private bool _startMinimized;
    public bool StartMinimized { get => _startMinimized; set { if (Set(ref _startMinimized, value)) OnSettingsChanged(); } }

    private string _language = "en";
    public string Language { get => _language; set { if (Set(ref _language, value)) OnSettingsChanged(); } }

    private bool _zoomSmoothing = true;
    public bool ZoomSmoothing { get => _zoomSmoothing; set { if (Set(ref _zoomSmoothing, value)) OnSettingsChanged(); } }

    private bool _momentumEnabled;
    public bool MomentumEnabled { get => _momentumEnabled; set { if (Set(ref _momentumEnabled, value)) OnSettingsChanged(); } }

    private int _momentumFriction = 50;
    public int MomentumFriction { get => _momentumFriction; set { if (Set(ref _momentumFriction, value)) OnSettingsChanged(); } }

    private bool _middleClickScroll = true;
    public bool MiddleClickScroll { get => _middleClickScroll; set { if (Set(ref _middleClickScroll, value)) OnSettingsChanged(); } }

    private int _middleClickDeadZone = 10;
    public int MiddleClickDeadZone { get => _middleClickDeadZone; set { if (Set(ref _middleClickDeadZone, value)) OnSettingsChanged(); } }

    public ObservableCollection<string> ExcludedApps { get; }

    public void AddExcludedApp(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName)) return;
        if (!ExcludedApps.Contains(appName, StringComparer.OrdinalIgnoreCase))
        {
            ExcludedApps.Add(appName);
            OnSettingsChanged();
        }
    }

    public void RemoveExcludedApp(string appName)
    {
        if (ExcludedApps.Remove(appName))
        {
            OnSettingsChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SettingsChanged;

    private void OnSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
