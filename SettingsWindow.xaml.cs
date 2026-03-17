using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace SoftScroll;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    private static readonly Regex _numRegex = new("^[0-9]+$");
    private int _activeTab;

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
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

        // Excluded Apps tab
        TxtAppsTitle.Text = L("ExcludedAppsTitle");
        TxtAppsDesc.Text  = L("ExcludedAppsDesc");
        BtnAddApp.Content    = L("AddApp");
        BtnRemoveApp.Content = L("RemoveSelected");

        // About tab
        TxtAboutTitle.Text  = L("AboutTitle");
        TxtAboutDesc.Text   = L("AboutDesc");
        TxtMadeWith.Text    = L("MadeWith");
        TxtSystem.Text      = L("System");
        ChkStartWin.Content       = L("StartWithWindows");
        ChkStartMin.Content       = L("StartMinimized");
        TxtResetTitle.Text        = L("ResetTitle");
        TxtResetDesc.Text         = L("ResetDesc");
        BtnResetDefaults.Content  = L("ResetToDefaults");
        TxtLanguage.Text          = L("Language");

        // Footer
        TxtFooterHint.Text  = L("FooterHint");
        BtnClose.Content    = L("Close");
        BtnSave.Content     = L("Save");
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
        TabApps.Visibility      = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        TabAbout.Visibility     = index == 3 ? Visibility.Visible : Visibility.Collapsed;

        NavScrolling.Style = index == 0 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavBehavior.Style  = index == 1 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavApps.Style      = index == 2 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
        NavAbout.Style     = index == 3 ? (Style)Resources["NavButtonActive"] : (Style)Resources["NavButton"];
    }

    private void OnNavScrolling(object sender, RoutedEventArgs e) => SwitchTab(0);
    private void OnNavBehavior(object sender, RoutedEventArgs e)  => SwitchTab(1);
    private void OnNavApps(object sender, RoutedEventArgs e)      => SwitchTab(2);
    private void OnNavAbout(object sender, RoutedEventArgs e)     => SwitchTab(3);

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
}
