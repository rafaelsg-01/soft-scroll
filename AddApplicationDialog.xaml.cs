using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SoftScroll;

public partial class AddApplicationDialog : Window
{
    public string? SelectedProcessName { get; private set; }

    public AddApplicationDialog()
    {
        InitializeComponent();
        ApplyTheme();
        LoadRunningProcesses();
    }

    private void ApplyTheme()
    {
        bool isDark = ThemeHelper.IsDarkMode();

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
            SetBrush("HoverBrush", "#333333");
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
            SetBrush("HoverBrush", "#E8E8E8");
        }
    }

    private void SetBrush(string resourceKey, string colorHex)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
        Resources[resourceKey] = new System.Windows.Media.SolidColorBrush(color);
    }

    private void LoadRunningProcesses()
    {
        var processes = new List<ProcessInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    // Only include processes with main windows (visible apps)
                    if (proc.MainWindowHandle == IntPtr.Zero) continue;
                    if (string.IsNullOrEmpty(proc.ProcessName)) continue;
                    if (proc.ProcessName.Equals("SoftScroll", StringComparison.OrdinalIgnoreCase)) continue;
                    if (seen.Contains(proc.ProcessName)) continue;

                    seen.Add(proc.ProcessName);

                    ImageSource? icon = null;
                    try
                    {
                        var mainModule = proc.MainModule;
                        if (mainModule?.FileName != null)
                        {
                            icon = GetIconFromFile(mainModule.FileName);
                        }
                    }
                    catch
                    {
                        // Access denied for some processes
                    }

                    processes.Add(new ProcessInfo
                    {
                        Name = proc.ProcessName,
                        Icon = icon
                    });
                }
                catch
                {
                    // Skip processes we can't access
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddApplicationDialog] Error loading processes: {ex.Message}");
        }

        ProcessList.ItemsSource = processes.OrderBy(p => p.Name).ToList();
    }

    private ImageSource? GetIconFromFile(string filePath)
    {
        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
            if (icon == null) return null;

            using var bitmap = icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(20, 20));
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private void OnProcessDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OnAddClick(sender, e);
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (ProcessList.SelectedItem is ProcessInfo selected)
        {
            SelectedProcessName = selected.Name;
            DialogResult = true;
            Close();
        }
        else
        {
            System.Windows.MessageBox.Show("Please select an application from the list.", 
                "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Application",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedProcessName = Path.GetFileNameWithoutExtension(dialog.FileName);
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class ProcessInfo
    {
        public string Name { get; set; } = "";
        public ImageSource? Icon { get; set; }
    }
}
