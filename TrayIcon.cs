using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace SmoothScrollClone;

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

        _openSettingsItem = new ToolStripMenuItem("Configuracoes...");
        _openSettingsItem.Click += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        _enabledItem = new ToolStripMenuItem("Ativado") { Checked = _settings.Enabled, CheckOnClick = true };
        _enabledItem.CheckedChanged += (s, e) => EnabledToggled?.Invoke(this, _enabledItem.Checked);

        _exitItem = new ToolStripMenuItem("Sair");
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
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var icoPath = Path.Combine(exeDir, "Assets", "app.ico");
            if (File.Exists(icoPath))
                return new System.Drawing.Icon(icoPath);
        }
        catch { }
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
