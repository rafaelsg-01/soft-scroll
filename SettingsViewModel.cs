using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmoothScrollClone;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public SettingsViewModel(AppSettings settings)
    {
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
        ShiftKeyHorizontal = s.ShiftKeyHorizontal;
        HorizontalSmoothness = s.HorizontalSmoothness;
        ReverseWheelDirection = s.ReverseWheelDirection;
        StartWithWindows = s.StartWithWindows;
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
        ShiftKeyHorizontal = ShiftKeyHorizontal,
        HorizontalSmoothness = HorizontalSmoothness,
        ReverseWheelDirection = ReverseWheelDirection,
        StartWithWindows = StartWithWindows
    };

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

    private bool _shiftHorizontal;
    public bool ShiftKeyHorizontal { get => _shiftHorizontal; set { if (Set(ref _shiftHorizontal, value)) OnSettingsChanged(); } }

    private bool _horizontalSmooth;
    public bool HorizontalSmoothness { get => _horizontalSmooth; set { if (Set(ref _horizontalSmooth, value)) OnSettingsChanged(); } }

    private bool _reverse;
    public bool ReverseWheelDirection { get => _reverse; set { if (Set(ref _reverse, value)) OnSettingsChanged(); } }

    private bool _startWithWindows;
    public bool StartWithWindows { get => _startWithWindows; set { if (Set(ref _startWithWindows, value)) OnSettingsChanged(); } }

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
