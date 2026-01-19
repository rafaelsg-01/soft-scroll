using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;

namespace SoftScroll;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _openSettingsItem;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _exitItem;

    private readonly AppSettings _settings;

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler<bool>? EnabledToggled;

    public TrayIcon(AppSettings settings)
    {
        _settings = settings;

        _openSettingsItem = new ToolStripMenuItem("Settings...");
        _openSettingsItem.Click += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        _enabledItem = new ToolStripMenuItem("Enabled") { Checked = _settings.Enabled, CheckOnClick = true };
        _enabledItem.CheckedChanged += (s, e) => EnabledToggled?.Invoke(this, _enabledItem.Checked);

        _exitItem = new ToolStripMenuItem("Exit");
        _exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var cms = new ContextMenuStrip();
        cms.Items.Add(_openSettingsItem);
        cms.Items.Add(new ToolStripSeparator());
        cms.Items.Add(_enabledItem);
        cms.Items.Add(new ToolStripSeparator());
        cms.Items.Add(_exitItem);

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Soft Scroll",
            ContextMenuStrip = cms,
            Icon = LoadIconSafe()
        };
        _notifyIcon.DoubleClick += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
        _notifyIcon.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) OpenSettingsRequested?.Invoke(this, EventArgs.Empty); };
    }

    private static System.Drawing.Icon LoadIconSafe()
    {
        // 1) Try WPF embedded resource (works in single-file publish)
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
            var sri = System.Windows.Application.GetResourceStream(uri);
            if (sri != null)
                return new System.Drawing.Icon(sri.Stream);
        }
        catch (Exception ex) { Debug.WriteLine($"[TrayIcon] Failed to load embedded icon: {ex.Message}"); }

        // 2) Fallback to EXE associated icon (uses ApplicationIcon)
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule!.FileName!;
            var ico = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            if (ico != null) return ico;
        }
        catch (Exception ex) { Debug.WriteLine($"[TrayIcon] Failed to extract EXE icon: {ex.Message}"); }

        // 3) Last resort: default app icon
        return System.Drawing.SystemIcons.Application;
    }

    public void UpdateEnabled(bool enabled)
    {
        _enabledItem.Checked = enabled;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
