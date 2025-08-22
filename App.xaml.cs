using System;
using System.Windows;

namespace SmoothScrollClone
{
    public partial class App : System.Windows.Application
    {
        private TrayIcon? _tray;
        private GlobalMouseHook? _hook;
        private SettingsViewModel? _vm;
        private SmoothScrollEngine? _engine;
        private SettingsWindow? _settingsWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = AppSettings.Load();
            _vm = new SettingsViewModel(settings);
            _vm.SettingsChanged += (_, __) =>
            {
                _tray?.UpdateEnabled(_vm.Enabled);
                if (_engine != null) _engine.ApplySettings(_vm.Snapshot());
            };

            _tray = new TrayIcon(settings);
            _tray.OpenSettingsRequested += (_, __) => ShowSettingsWindow();
            _tray.ExitRequested += (_, __) => Shutdown();
            _tray.EnabledToggled += (_, enabled) =>
            {
                if (_vm is null) return;
                _vm.Enabled = enabled;
                settings.Enabled = enabled;
                settings.Save();
                UpdateHookState();
            };

            _engine = new SmoothScrollEngine(settings);

            _hook = new GlobalMouseHook();
            _hook.MouseWheel += (_, args) =>
            {
                if (!settings.Enabled) return;
                args.Handled = true;
                _engine!.OnWheel(args.Delta);
            };
            _hook.MouseHWheel += (_, args) =>
            {
                if (!settings.Enabled) return;
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
                _engine.ApplySettings(_vm.Snapshot());
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
