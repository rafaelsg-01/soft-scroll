using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmoothScrollClone;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    private static readonly Regex _numRegex = new("^[0-9]+$");

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        this.Title = "Soft Scroll - Configuracoes";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var snapshot = _vm.Snapshot();
        snapshot.Save();
        this.Title = "Soft Scroll - Configuracoes (salvo)";
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
