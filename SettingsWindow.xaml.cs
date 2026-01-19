using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SoftScroll;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    private static readonly Regex _numRegex = new("^[0-9]+$");
    private string? _lastFocusedApp;

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        this.Title = "Soft Scroll - Settings";

        // Capture the last focused app before this window was opened
        _lastFocusedApp = ProcessHelper.GetForegroundProcessName();
        if (string.Equals(_lastFocusedApp, "SoftScroll", StringComparison.OrdinalIgnoreCase))
        {
            _lastFocusedApp = null;
        }

        // Apply theme based on Windows settings
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        bool isDark = ThemeHelper.IsDarkMode();

        // Update resource dictionary colors
        if (isDark)
        {
            SetBrush("BackgroundBrush", ThemeHelper.Dark.Background);
            SetBrush("SurfaceBrush", ThemeHelper.Dark.Surface);
            SetBrush("SurfaceBorderBrush", ThemeHelper.Dark.SurfaceBorder);
            SetBrush("TextBrush", ThemeHelper.Dark.Text);
            SetBrush("TextSecondaryBrush", ThemeHelper.Dark.TextSecondary);
            SetBrush("AccentBrush", ThemeHelper.Dark.Accent);
            SetBrush("InputBrush", ThemeHelper.Dark.Input);
            SetBrush("InputBorderBrush", ThemeHelper.Dark.InputBorder);
        }
        else
        {
            SetBrush("BackgroundBrush", ThemeHelper.Light.Background);
            SetBrush("SurfaceBrush", ThemeHelper.Light.Surface);
            SetBrush("SurfaceBorderBrush", ThemeHelper.Light.SurfaceBorder);
            SetBrush("TextBrush", ThemeHelper.Light.Text);
            SetBrush("TextSecondaryBrush", ThemeHelper.Light.TextSecondary);
            SetBrush("AccentBrush", ThemeHelper.Light.Accent);
            SetBrush("InputBrush", ThemeHelper.Light.Input);
            SetBrush("InputBorderBrush", ThemeHelper.Light.InputBorder);
        }
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

        // Update Windows startup registry based on setting
        StartupManager.SetStartup(snapshot.StartWithWindows);

        this.Title = "Soft Scroll - Settings (saved)";
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        _vm.Apply(AppSettings.CreateDefault());
    }

    // Close button -> behaves like normal window close (app stays in tray)
    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Add app to exclusion list - opens dialog to select from running apps
    private void OnAddApp(object sender, RoutedEventArgs e)
    {
        var dialog = new AddApplicationDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedProcessName))
        {
            _vm.AddExcludedApp(dialog.SelectedProcessName);
        }
    }

    // Remove selected app from exclusion list
    private void OnRemoveApp(object sender, RoutedEventArgs e)
    {
        if (ExcludedAppsList.SelectedItem is string selected)
        {
            _vm.RemoveExcludedApp(selected);
        }
        else
        {
            System.Windows.MessageBox.Show("Please select an application from the list to remove.", 
                "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Numeric input filters
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
