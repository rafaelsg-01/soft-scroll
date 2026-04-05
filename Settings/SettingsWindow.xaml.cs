using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SoftScroll.Infrastructure;
using SoftScroll.ViewModels;
using SoftScroll.UI;

namespace SoftScroll.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    private readonly StatisticsViewModel _statsVm;
    private static readonly Regex _numRegex = new("^[0-9]+$");
    private int _activeTab;

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        _statsVm = new StatisticsViewModel();
        DataContext = _vm;

        LocalizationManager.SetLanguage(_vm.Language);

        LanguageCombo.ItemsSource = LocalizationManager.LanguageDisplayNames;
        LanguageCombo.SelectedIndex = LocalizationManager.GetLanguageIndex();

        ApplyTheme();
        ApplyLocalization();
        SwitchTab(0);
    }

    private void ApplyLocalization()
    {
        var L = new Func<string, string>(LocalizationManager.Get);

        this.Title = L("WindowTitle");

        // Nav
        NavScrolling.Content = L("NavScrolling");
        NavBehavior.Content  = L("NavBehavior");
        NavAppProfiles.Content = L("NavAppProfiles");
        NavVisual.Content = L("NavVisual");
        NavAccessibility.Content = L("NavAccessibility");
        NavStats.Content = L("NavStats");
        NavApps.Content      = L("NavExcludedApps");
        NavAbout.Content     = L("NavAbout");

        // Scrolling tab
        TxtScrollingTitle.Text = L("ScrollingTitle");
        TxtScrollingDesc.Text  = L("ScrollingDesc");
        TxtQuickPresets.Text   = L("QuickPresets");
        TxtQuickPresetsDesc.Text = L("QuickPresetsDesc");
        BtnPresetDefault.Content      = L("PresetDefault");
        BtnPresetReading.Content      = L("PresetReading");
        BtnPresetProductivity.Content = L("PresetProductivity");
        BtnPresetGaming.Content       = L("PresetGaming");
        TxtParameters.Text    = L("Parameters");
        TxtStepSize.Text      = L("StepSize");
        TxtAnimTime.Text      = L("AnimationTime");
        TxtAccelDelta.Text    = L("AccelerationDelta");
        TxtAccelMax.Text      = L("AccelerationMax");
        TxtTailHead.Text      = L("TailToHeadRatio");
        TxtEasing.Text        = L("EasingCurve");

        // Behavior tab
        TxtBehaviorTitle.Text = L("BehaviorTitle");
        TxtBehaviorDesc.Text  = L("BehaviorDesc");
        TxtScrollFeatures.Text = L("ScrollFeatures");
        ChkEnableSmooth.Content   = L("EnableSmoothScrolling");
        ChkAnimEasing.Content     = L("AnimationEasing");
        TxtHorizontal.Text        = L("HorizontalScrolling");
        ChkShiftHoriz.Content     = L("ShiftKeyHorizontal");
        ChkSmoothHoriz.Content    = L("SmoothHorizontal");
        TxtDirection.Text         = L("Direction");
        ChkReverse.Content        = L("ReverseWheel");

        // Advanced features (Behavior tab)
        TxtAdvancedFeatures.Text  = L("AdvancedFeatures");
        ChkZoomSmoothing.Content  = L("ZoomSmoothing");
        ChkMiddleClickScroll.Content = L("MiddleClickScroll");
        TxtMomentum.Text          = L("MomentumScrolling");
        TxtMomentumDesc.Text      = L("MomentumDesc");
        ChkMomentum.Content       = L("EnableMomentum");
        TxtFriction.Text          = L("Friction");

        // Curve preview (Scrolling tab)
        TxtCurvePreview.Text      = L("CurvePreview");

        // Visual Feedback tab
        TxtVisualTitle.Text = L("VisualFeedbackTitle");
        TxtVisualDesc.Text = L("VisualFeedbackDesc");
        TxtScrollIndicatorLabel.Text = L("ScrollIndicator");
        ChkShowScrollIndicator.Content = L("ShowScrollIndicator");
        TxtIndicatorPositionLabel.Text = L("IndicatorPosition");
        TxtIndicatorDurationLabel.Text = L("IndicatorDuration");
        TxtMsLabel.Text = "ms";
        TxtMiddleClickLabel.Text = L("MiddleClickOverlay");
        TxtMiddleClickDesc.Text = L("MiddleClickOverlayDesc");
        TxtCursorSizeLabel.Text = L("CursorSize");
        TxtPxLabel.Text = "px";
        TxtEdgeBounceLabel.Text = L("EdgeBounce");
        ChkEdgeBounce.Content = L("EnableEdgeBounce");
        TxtBounceStrengthLabel.Text = L("BounceStrength");

        // Accessibility tab
        TxtAccessibilityTitle.Text = L("AccessibilityTitle");
        TxtAccessibilityDesc.Text = L("AccessibilityDesc");
        TxtScreenReaderLabel.Text = L("ScreenReaderAnnouncements");
        ChkScreenReader.Content = L("EnableScreenReader");
        ChkAnnounceSpeed.Content = L("AnnounceSpeedChanges");
        ChkAnnouncePosition.Content = L("AnnounceScrollPosition");
        TxtHighContrastLabel.Text = L("HighContrastMode");
        ChkHighContrast.Content = L("EnableHighContrast");
        TxtHighContrastDesc.Text = L("HighContrastDesc");
        TxtAudioFeedbackLabel.Text = L("AudioFeedback");
        ChkAudioFeedback.Content = L("EnableAudioFeedback");
        TxtVolumeLabel.Text = L("Volume");
        TxtSoundTypeLabel.Text = L("SoundType");

        // Statistics tab
        TxtStatsTitle.Text = L("StatisticsTitle");
        TxtStatsDesc.Text = L("StatisticsDesc");
        TxtSessionStatsLabel.Text = L("SessionStatistics");
        TxtScrollEventsLabel.Text = L("ScrollEvents");
        TxtPixelsScrolledLabel.Text = L("PixelsScrolled");
        TxtSessionDurationLabel.Text = L("SessionDuration");
        TxtAllTimeStatsLabel.Text = L("AllTimeStatistics");
        TxtTotalEventsLabel.Text = L("TotalScrollEvents");
        TxtTotalPixelsLabel.Text = L("TotalPixels");
        BtnResetSession.Content = L("ResetSession");
        BtnResetAll.Content = L("ResetAllStatistics");

        // Excluded Apps tab
        TxtAppsTitle.Text = L("ExcludedAppsTitle");
        TxtAppsDesc.Text  = L("ExcludedAppsDesc");
        BtnAddApp.Content    = L("AddApp");
        BtnRemoveApp.Content = L("RemoveSelected");

        // App Profiles tab
        TxtAppProfilesTitle.Text = L("AppProfilesTitle");
        TxtAppProfilesDesc.Text = L("AppProfilesDesc");
        ChkUseAppProfiles.Content = L("EnableAppProfiles");
        TxtAppProfilesListHeader.Text = L("ApplicationProfiles");
        TxtStepLabel.Text = "Step:";
        TxtMsLabel.Text = "ms |";
        BtnAddProfile.Content = L("AddProfile");
        BtnRemoveProfile.Content = L("RemoveSelected");

        // About tab
        TxtAboutTitle.Text  = L("AboutTitle");
        TxtAboutDesc.Text   = L("AboutDesc");
        TxtAppName.Text = "Soft Scroll";
        TxtAppTagline.Text = L("AppTagline");
        TxtVersion.Text = "Version 0.3.0";
        TxtMadeWith.Text    = L("MadeWith");
        TxtLanguageLabel.Text = L("Language");
        TxtSystemLabel.Text      = L("System");
        ChkStartWin.Content       = L("StartWithWindows");
        ChkStartMin.Content       = L("StartMinimized");
        TxtResetLabel.Text        = L("ResetTitle");
        TxtResetDesc.Text         = L("ResetDesc");
        BtnResetDefaults.Content  = L("ResetToDefaults");

        // Footer
        TxtFooterHint.Text  = L("FooterHint");
        BtnClose.Content    = L("Close");
        BtnSave.Content     = L("Save");

        // ComboBox items localization
        PosTopRight.Content = "Top Right";
        PosTopLeft.Content = "Top Left";
        PosBottomRight.Content = "Bottom Right";
        PosBottomLeft.Content = "Bottom Left";
        PosCenter.Content = "Center";

        SoundSoftTick.Content = L("SoundSoftTick");
        SoundClick.Content = L("SoundClick");
        SoundCustom.Content = L("SoundCustom");
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageCombo.SelectedIndex < 0) return;
        var langCode = LocalizationManager.SupportedLanguages[LanguageCombo.SelectedIndex];
        _vm.Language = langCode;
        LocalizationManager.SetLanguage(langCode);
        ApplyLocalization();

        // Re-apply active tab style
        SwitchTab(_activeTab);
    }

    private void SwitchTab(int index)
    {
        _activeTab = index;

        TabScrolling.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        TabBehavior.Visibility  = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        TabAppProfiles.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        TabVisual.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        TabAccessibility.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
        TabStats.Visibility = index == 5 ? Visibility.Visible : Visibility.Collapsed;
        TabApps.Visibility      = index == 6 ? Visibility.Visible : Visibility.Collapsed;
        TabAbout.Visibility     = index == 7 ? Visibility.Visible : Visibility.Collapsed;

        NavScrolling.Style = index == 0 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavBehavior.Style  = index == 1 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavAppProfiles.Style = index == 2 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavVisual.Style = index == 3 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavAccessibility.Style = index == 4 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavStats.Style = index == 5 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavApps.Style      = index == 6 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavAbout.Style     = index == 7 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];

        if (index == 5) RefreshStatistics();
    }

    private void OnNavScrolling(object sender, RoutedEventArgs e) => SwitchTab(0);
    private void OnNavBehavior(object sender, RoutedEventArgs e)  => SwitchTab(1);
    private void OnNavAppProfiles(object sender, RoutedEventArgs e) => SwitchTab(2);
    private void OnNavVisual(object sender, RoutedEventArgs e) => SwitchTab(3);
    private void OnNavAccessibility(object sender, RoutedEventArgs e) => SwitchTab(4);
    private void OnNavStats(object sender, RoutedEventArgs e) => SwitchTab(5);
    private void OnNavApps(object sender, RoutedEventArgs e)      => SwitchTab(6);
    private void OnNavAbout(object sender, RoutedEventArgs e)     => SwitchTab(7);

    [DllImport("DwmApi")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        UpdateTitleBarTheme();
    }

    private void UpdateTitleBarTheme()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            int[] darkAttr = new int[] { ThemeHelper.IsDarkMode() ? 1 : 0 };
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, darkAttr, 4);
        }
    }

    private void ApplyTheme()
    {
        bool isDark = ThemeHelper.IsDarkMode();
        var t = isDark ? typeof(ThemeHelper.Dark) : typeof(ThemeHelper.Light);

        SetBrush("BackgroundBrush",    GetColor(t, "Background"));
        SetBrush("SurfaceBrush",       GetColor(t, "Surface"));
        SetBrush("SurfaceHoverBrush",  GetColor(t, "SurfaceHover"));
        SetBrush("SurfaceActiveBrush", GetColor(t, "SurfaceActive"));
        SetBrush("SurfaceBorderBrush", GetColor(t, "SurfaceBorder"));
        SetBrush("TextBrush",          GetColor(t, "Text"));
        SetBrush("TextSecondaryBrush", GetColor(t, "TextSecondary"));
        SetBrush("TextTertiaryBrush",  GetColor(t, "TextTertiary"));
        SetBrush("AccentBrush",        GetColor(t, "Accent"));
        SetBrush("AccentHoverBrush",   GetColor(t, "AccentHover"));
        SetBrush("AccentTextBrush",    GetColor(t, "AccentText"));
        SetBrush("InputBrush",         GetColor(t, "Input"));
        SetBrush("InputBorderBrush",   GetColor(t, "InputBorder"));
        SetBrush("InputFocusBrush",    GetColor(t, "InputFocus"));
        SetBrush("NavBackgroundBrush", GetColor(t, "NavBackground"));
        SetBrush("NavSelectedBrush",   GetColor(t, "NavSelected"));
        SetBrush("NavIndicatorBrush",  GetColor(t, "NavIndicator"));
        
        UpdateTitleBarTheme();
    }

    private static string GetColor(Type themeClass, string fieldName)
    {
        var field = themeClass.GetField(fieldName);
        return (string)(field?.GetValue(null) ?? "#FF00FF");
    }

    private void SetBrush(string resourceKey, string colorHex)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
        Resources[resourceKey] = new System.Windows.Media.SolidColorBrush(color);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var snapshot = _vm.Snapshot();
        snapshot.Save();
        StartupManager.SetStartup(snapshot.StartWithWindows);
        this.Title = LocalizationManager.Get("WindowTitleSaved");
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        _vm.Apply(AppSettings.CreateDefault());
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    private void OnPresetDefault(object sender, RoutedEventArgs e)      => _vm.ApplyPreset("Default");
    private void OnPresetReading(object sender, RoutedEventArgs e)      => _vm.ApplyPreset("Reading");
    private void OnPresetProductivity(object sender, RoutedEventArgs e) => _vm.ApplyPreset("Productivity");
    private void OnPresetGaming(object sender, RoutedEventArgs e)       => _vm.ApplyPreset("Gaming");

    private void OnAddApp(object sender, RoutedEventArgs e)
    {
        var dialog = new AddApplicationDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedProcessName))
        {
            _vm.AddExcludedApp(dialog.SelectedProcessName);
        }
    }

    private void OnRemoveApp(object sender, RoutedEventArgs e)
    {
        if (ExcludedAppsList.SelectedItem is string selected)
        {
            _vm.RemoveExcludedApp(selected);
        }
        else
        {
            System.Windows.MessageBox.Show(
                LocalizationManager.Get("NoSelectionMsg"),
                LocalizationManager.Get("NoSelectionTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private void OnAddAppProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new AddApplicationDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedProcessName))
        {
            var settings = _vm.Snapshot();
            var appName = dialog.SelectedAppName ?? dialog.SelectedProcessName ?? "Unknown";
            var processName = dialog.SelectedProcessName ?? "Unknown";
            var profile = AppProfile.FromAppSettings(appName, processName, settings);
            _vm.AddAppProfile(profile);
        }
    }

    private void OnRemoveAppProfile(object sender, RoutedEventArgs e)
    {
        if (AppProfilesList.SelectedItem is AppProfile selected)
        {
            _vm.RemoveAppProfile(selected);
        }
        else
        {
            System.Windows.MessageBox.Show(
                LocalizationManager.Get("NoSelectionMsg"),
                LocalizationManager.Get("NoSelectionTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private void NumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !_numRegex.IsMatch(e.Text);
    }

    private void OnPasteNumeric(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
        {
            var text = (string)e.DataObject.GetData(System.Windows.DataFormats.Text);
            if (!_numRegex.IsMatch(text)) e.CancelCommand();
        }
        else e.CancelCommand();
    }

    private void RefreshStatistics()
    {
        _statsVm.RefreshAll();
        TxtSessionEvents.Text = _statsVm.SessionEvents;
        TxtSessionPixels.Text = _statsVm.SessionPixels;
        TxtSessionTime.Text = _statsVm.ActiveTime;
        TxtTotalEvents.Text = _statsVm.TotalEvents;
        TxtTotalPixels.Text = _statsVm.TotalPixels;
    }

    private void OnResetSession(object sender, RoutedEventArgs e)
    {
        _statsVm.ResetSession();
        RefreshStatistics();
    }

    private void OnResetAll(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            LocalizationManager.Get("ResetStatisticsConfirm"),
            LocalizationManager.Get("ResetStatisticsTitle"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _statsVm.ResetAll();
            RefreshStatistics();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _statsVm.Stop();
        base.OnClosed(e);
    }
}
