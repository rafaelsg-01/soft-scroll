using System;
using System.Diagnostics;
using System.Windows;

namespace SoftScroll
{
    public partial class App : System.Windows.Application
    {
        private TrayIcon? _tray;
        private GlobalMouseHook? _hook;
        private SettingsViewModel? _vm;
        private SmoothScrollEngine? _engine;
        private SettingsWindow? _settingsWindow;
        private AppSettings _settings = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _settings = AppSettings.Load();
            _vm = new SettingsViewModel(_settings);
            _vm.SettingsChanged += (_, __) =>
            {
                _tray?.UpdateEnabled(_vm.Enabled);
                var snapshot = _vm.Snapshot();
                _settings = snapshot;
                if (_engine != null) _engine.ApplySettings(snapshot);
                if (_hook != null) _hook.ShiftKeyHorizontal = snapshot.ShiftKeyHorizontal;
            };

            _tray = new TrayIcon(_settings);
            _tray.OpenSettingsRequested += (_, __) => ShowSettingsWindow();
            _tray.ExitRequested += (_, __) => Shutdown();
            _tray.EnabledToggled += (_, enabled) =>
            {
                if (_vm is null) return;
                _vm.Enabled = enabled;
                _settings.Enabled = enabled;
                _settings.Save();
                UpdateHookState();
            };

            _engine = new SmoothScrollEngine(_settings);

            _hook = new GlobalMouseHook();
            _hook.ShiftKeyHorizontal = _settings.ShiftKeyHorizontal;

            _hook.MouseWheel += (_, args) =>
            {
                if (!_settings.Enabled) return;

                // Check per-app exclusion list - use window under cursor for accurate detection
                var foregroundProcess = ProcessHelper.GetProcessUnderCursor();
                if (_settings.IsExcluded(foregroundProcess))
                {
                    return; // Skip smooth scrolling, let original event pass through
                }

                args.Handled = true;
                _engine!.OnWheel(args.Delta);
            };
            _hook.MouseHWheel += (_, args) =>
            {
                if (!_settings.Enabled) return;

                // Check per-app exclusion list - use window under cursor
                var foregroundProcess = ProcessHelper.GetProcessUnderCursor();
                if (_settings.IsExcluded(foregroundProcess)) return;

                args.Handled = true;
                _engine!.OnHWheel(args.Delta);
            };

            UpdateHookState();

            // Open settings on startup and keep reference
            ShowSettingsWindow();
            Current.MainWindow = _settingsWindow;
        }

        private void UpdateHookState()
        {
            if (_hook is null || _vm is null || _engine is null) return;
            if (_vm.Enabled)
            {
                var snapshot = _vm.Snapshot();
                _engine.ApplySettings(snapshot);
                _hook.ShiftKeyHorizontal = snapshot.ShiftKeyHorizontal;
                _engine.Start();
                _hook.Install();
            }
            else
            {
                _hook.Uninstall();
                _engine.Stop();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _hook?.Dispose();
            _engine?.Dispose();
            _tray?.Dispose();
        }

        private void ShowSettingsWindow()
        {
            if (_vm is null) return;
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(_vm);
                _settingsWindow.Closed += (_, __) => _settingsWindow = null;
                _settingsWindow.Owner = null;
                _settingsWindow.Show();
            }
            else
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                    _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
            }
        }
    }
}
