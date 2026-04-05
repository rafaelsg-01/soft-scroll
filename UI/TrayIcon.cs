using System;
using System.Drawing;
using System.Windows.Forms;
using SoftScroll.Infrastructure;
using SoftScroll.Settings;

namespace SoftScroll.UI;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _openSettingsItem;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _toggleHotkeyItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly AppSettings _settings;

    private Icon? _iconEnabled;
    private Icon? _iconDisabled;

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler<bool>? EnabledToggled;
    public event EventHandler? ToggleHotkeyRequested;

    public TrayIcon(AppSettings settings)
    {
        _settings = settings;

        // Load icons - use same icon but will change text/brightness based on state
        _iconEnabled = LoadIconSafe();
        _iconDisabled = CreateDisabledIcon(_iconEnabled);

        var L = new Func<string, string>(LocalizationManager.Get);

        _openSettingsItem = new ToolStripMenuItem(L("TraySettings"));
        _openSettingsItem.Click += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        _enabledItem = new ToolStripMenuItem(L("TrayEnabled")) { Checked = _settings.Enabled, CheckOnClick = true };
        _enabledItem.CheckedChanged += (s, e) => EnabledToggled?.Invoke(this, _enabledItem.Checked);

        _toggleHotkeyItem = new ToolStripMenuItem(L("TrayToggleHotkey"));
        _toggleHotkeyItem.Click += (s, e) => ToggleHotkeyRequested?.Invoke(this, EventArgs.Empty);

        _exitItem = new ToolStripMenuItem(L("TrayExit"));
        _exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var cms = new ContextMenuStrip();
        cms.Items.Add(_openSettingsItem);
        cms.Items.Add(new ToolStripSeparator());
        cms.Items.Add(_enabledItem);
        cms.Items.Add(_toggleHotkeyItem);
        cms.Items.Add(new ToolStripSeparator());
        cms.Items.Add(_exitItem);

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = GetTrayTooltip(),
            ContextMenuStrip = cms,
            Icon = _settings.Enabled ? _iconEnabled : _iconDisabled
        };

        _notifyIcon.DoubleClick += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        // Left-click now toggles enabled state instead of opening settings
        _notifyIcon.MouseUp += OnMouseUp;
    }

    private string GetTrayTooltip()
    {
        var L = new Func<string, string>(LocalizationManager.Get);
        return _settings.Enabled 
            ? $"{L("AppName")} ({L("TrayEnabled")})" 
            : $"{L("AppName")} ({L("TrayDisabled")})";
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Toggle enabled state on left click
            var newState = !_settings.Enabled;
            _settings.Enabled = newState;
            _settings.Save();
            UpdateEnabled(newState);
            EnabledToggled?.Invoke(this, newState);
        }
    }

    private static Icon LoadIconSafe()
    {
        // 1) Try WPF embedded resource (works in single-file publish)
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
            var sri = System.Windows.Application.GetResourceStream(uri);
            if (sri != null)
                return new Icon(sri.Stream);
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "[TrayIcon] Failed to load embedded icon"); }

        // 2) Fallback to EXE associated icon (uses ApplicationIcon)
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
            var ico = Icon.ExtractAssociatedIcon(exePath);
            if (ico != null) return ico;
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "[TrayIcon] Failed to extract EXE icon"); }

        // 3) Last resort: default app icon
        return SystemIcons.Application;
    }

    private static Icon CreateDisabledIcon(Icon? original)
    {
        if (original == null) return SystemIcons.Application;

        // Create a grayed-out version for disabled state
        using var bitmap = original.ToBitmap();
        var disabledBitmap = new Bitmap(bitmap.Width, bitmap.Height);

        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0)
                {
                    var gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                    disabledBitmap.SetPixel(x, y, Color.FromArgb(pixel.A, gray, gray, gray));
                }
            }
        }

        return Icon.FromHandle(disabledBitmap.GetHicon());
    }

    public void UpdateEnabled(bool enabled)
    {
        _enabledItem.Checked = enabled;
        _notifyIcon.Text = GetTrayTooltip();
        _notifyIcon.Icon = enabled ? _iconEnabled : _iconDisabled;
    }

    public void RefreshLocalization()
    {
        var L = new Func<string, string>(LocalizationManager.Get);
        _openSettingsItem.Text = L("TraySettings");
        _enabledItem.Text = L("TrayEnabled");
        _toggleHotkeyItem.Text = L("TrayToggleHotkey");
        _exitItem.Text = L("TrayExit");
        _notifyIcon.Text = GetTrayTooltip();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _iconEnabled?.Dispose();
        _iconDisabled?.Dispose();
    }
}
