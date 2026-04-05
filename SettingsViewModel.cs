using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SoftScroll;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public SettingsViewModel(AppSettings settings)
    {
        ExcludedApps = new ObservableCollection<string>();
        AppProfiles = new ObservableCollection<AppProfile>();
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
        UseAppProfiles = s.UseAppProfiles;

        // Visual feedback settings
        ShowScrollIndicator = s.ShowScrollIndicator;
        ScrollIndicatorDurationMs = s.ScrollIndicatorDurationMs;
        IndicatorPosition = s.IndicatorPosition;

        // Middle-click settings
        MiddleClickCursorStyle = s.MiddleClickConfig.CursorStyle;
        MiddleClickCursorSize = s.MiddleClickConfig.CursorSize;
        MiddleClickInvertDirection = s.MiddleClickConfig.InvertScrollDirection;
        MiddleClickEnableEdgeBounce = s.MiddleClickConfig.EnableEdgeBounce;
        MiddleClickBounceStrength = s.MiddleClickConfig.BounceStrength;

        // Accessibility settings
        EnableScreenReaderAnnouncements = s.Accessibility.EnableScreenReaderAnnouncements;
        AnnounceScrollSpeed = s.Accessibility.AnnounceScrollSpeed;
        AnnounceScrollPosition = s.Accessibility.AnnounceScrollPosition;
        HighContrastMode = s.Accessibility.HighContrastMode;
        EnableAudioFeedback = s.Accessibility.EnableAudioFeedback;
        AudioVolume = s.Accessibility.AudioVolume;
        ScrollSound = s.Accessibility.ScrollSound;

        ExcludedApps.Clear();
        foreach (var app in s.ExcludedApps)
            ExcludedApps.Add(app);

        AppProfiles.Clear();
        foreach (var profile in s.AppProfiles)
            AppProfiles.Add(profile);
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
        ExcludedApps = new List<string>(ExcludedApps),
        UseAppProfiles = UseAppProfiles,
        AppProfiles = new List<AppProfile>(AppProfiles),
        ShowScrollIndicator = ShowScrollIndicator,
        ScrollIndicatorDurationMs = ScrollIndicatorDurationMs,
        IndicatorPosition = IndicatorPosition,
        MiddleClickConfig = new MiddleClickSettings
        {
            CursorStyle = MiddleClickCursorStyle,
            CursorSize = MiddleClickCursorSize,
            InvertScrollDirection = MiddleClickInvertDirection,
            EnableEdgeBounce = MiddleClickEnableEdgeBounce,
            BounceStrength = MiddleClickBounceStrength
        },
        Accessibility = new AccessibilitySettings
        {
            EnableScreenReaderAnnouncements = EnableScreenReaderAnnouncements,
            AnnounceScrollSpeed = AnnounceScrollSpeed,
            AnnounceScrollPosition = AnnounceScrollPosition,
            HighContrastMode = HighContrastMode,
            EnableAudioFeedback = EnableAudioFeedback,
            AudioVolume = AudioVolume,
            ScrollSound = ScrollSound
        }
    };

    public void ApplyPreset(string presetName)
    {
        var preset = PresetManager.FromPreset(presetName);
        StepSizePx = preset.StepSizePx;
        AnimationTimeMs = preset.AnimationTimeMs;
        AccelerationDeltaMs = preset.AccelerationDeltaMs;
        AccelerationMax = preset.AccelerationMax;
        TailToHeadRatio = preset.TailToHeadRatio;
        AnimationEasing = preset.AnimationEasing;
        EasingMode = preset.EasingMode;
        MomentumEnabled = preset.MomentumEnabled;
        MomentumFriction = preset.MomentumFriction;
    }

    public string[] GetPresetNames() => PresetManager.GetPresetNames();

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

    private bool _useAppProfiles = true;
    public bool UseAppProfiles { get => _useAppProfiles; set { if (Set(ref _useAppProfiles, value)) OnSettingsChanged(); } }

    // Visual feedback settings
    private bool _showScrollIndicator;
    public bool ShowScrollIndicator { get => _showScrollIndicator; set { if (Set(ref _showScrollIndicator, value)) OnSettingsChanged(); } }

    private int _scrollIndicatorDurationMs = 500;
    public int ScrollIndicatorDurationMs { get => _scrollIndicatorDurationMs; set { if (Set(ref _scrollIndicatorDurationMs, value)) OnSettingsChanged(); } }

    private IndicatorPosition _indicatorPosition = IndicatorPosition.TopRight;
    public IndicatorPosition IndicatorPosition { get => _indicatorPosition; set { if (Set(ref _indicatorPosition, value)) OnSettingsChanged(); } }

    // Middle-click custom settings
    private CursorStyle _middleClickCursorStyle = CursorStyle.Arrow;
    public CursorStyle MiddleClickCursorStyle { get => _middleClickCursorStyle; set { if (Set(ref _middleClickCursorStyle, value)) OnSettingsChanged(); } }

    private int _middleClickCursorSize = 32;
    public int MiddleClickCursorSize { get => _middleClickCursorSize; set { if (Set(ref _middleClickCursorSize, value)) OnSettingsChanged(); } }

    private bool _middleClickInvertDirection;
    public bool MiddleClickInvertDirection { get => _middleClickInvertDirection; set { if (Set(ref _middleClickInvertDirection, value)) OnSettingsChanged(); } }

    private bool _middleClickEnableEdgeBounce = true;
    public bool MiddleClickEnableEdgeBounce { get => _middleClickEnableEdgeBounce; set { if (Set(ref _middleClickEnableEdgeBounce, value)) OnSettingsChanged(); } }

    private int _middleClickBounceStrength = 20;
    public int MiddleClickBounceStrength { get => _middleClickBounceStrength; set { if (Set(ref _middleClickBounceStrength, value)) OnSettingsChanged(); } }

    // Accessibility settings
    private bool _enableScreenReaderAnnouncements;
    public bool EnableScreenReaderAnnouncements { get => _enableScreenReaderAnnouncements; set { if (Set(ref _enableScreenReaderAnnouncements, value)) OnSettingsChanged(); } }

    private bool _announceScrollSpeed;
    public bool AnnounceScrollSpeed { get => _announceScrollSpeed; set { if (Set(ref _announceScrollSpeed, value)) OnSettingsChanged(); } }

    private bool _announceScrollPosition = true;
    public bool AnnounceScrollPosition { get => _announceScrollPosition; set { if (Set(ref _announceScrollPosition, value)) OnSettingsChanged(); } }

    private bool _highContrastMode;
    public bool HighContrastMode { get => _highContrastMode; set { if (Set(ref _highContrastMode, value)) OnSettingsChanged(); } }

    private bool _enableAudioFeedback;
    public bool EnableAudioFeedback { get => _enableAudioFeedback; set { if (Set(ref _enableAudioFeedback, value)) OnSettingsChanged(); } }

    private float _audioVolume = 0.5f;
    public float AudioVolume { get => _audioVolume; set { if (Set(ref _audioVolume, value)) OnSettingsChanged(); } }

    private SoundType _scrollSound = SoundType.SoftTick;
    public SoundType ScrollSound { get => _scrollSound; set { if (Set(ref _scrollSound, value)) OnSettingsChanged(); } }

    public ObservableCollection<string> ExcludedApps { get; }
    public ObservableCollection<AppProfile> AppProfiles { get; }

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

    public void AddAppProfile(AppProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.ProcessName)) return;
        if (!AppProfiles.Any(p => p.ProcessName.Equals(profile.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            AppProfiles.Add(profile);
            OnSettingsChanged();
        }
    }

    public void RemoveAppProfile(AppProfile profile)
    {
        if (AppProfiles.Remove(profile))
        {
            OnSettingsChanged();
        }
    }

    public void UpdateAppProfile(string processName, AppProfile updatedProfile)
    {
        var existing = AppProfiles.FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            var index = AppProfiles.IndexOf(existing);
            AppProfiles[index] = updatedProfile;
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
