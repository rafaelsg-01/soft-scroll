using System;
using System.Windows;
using Serilog;

namespace SoftScroll;

public partial class App : System.Windows.Application
{
    private TrayIcon? _tray;
    private GlobalMouseHook? _hook;
    private SettingsViewModel? _vm;
    private SmoothScrollEngine? _engine;
    private ZoomSmoothEngine? _zoomEngine;
    private MiddleClickScrollEngine? _middleClickEngine;
    private MiddleClickOverlay? _middleClickOverlay;
    private SettingsWindow? _settingsWindow;
    private AppSettings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        LoggingConfig.Configure();
        
        base.OnStartup(e);

        _settings = AppSettings.Load();
        _vm = new SettingsViewModel(_settings);
        _vm.SettingsChanged += (_, _) =>
        {
            _tray?.UpdateEnabled(_vm.Enabled);
            var snapshot = _vm.Snapshot();
            _settings = snapshot;
            _engine?.ApplySettings(snapshot);
            _middleClickEngine?.UpdateDeadZone(snapshot.MiddleClickDeadZone);
            if (_hook != null) _hook.ShiftKeyHorizontal = snapshot.ShiftKeyHorizontal;
        };

        _tray = new TrayIcon(_settings);
        _tray.OpenSettingsRequested += (_, _) => ShowSettingsWindow();
        _tray.ExitRequested += (_, _) => Shutdown();
        _tray.EnabledToggled += (_, enabled) =>
        {
            if (_vm is null) return;
            _vm.Enabled = enabled;
            _settings.Enabled = enabled;
            _settings.Save();
            UpdateHookState();
        };

        _engine = new SmoothScrollEngine(_settings);
        _zoomEngine = new ZoomSmoothEngine();
        _middleClickEngine = new MiddleClickScrollEngine();

        _middleClickEngine.Activated += (x, y) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                _middleClickOverlay ??= new MiddleClickOverlay();
                _middleClickOverlay.ShowAt(x, y);
            });
        };
        _middleClickEngine.Deactivated += () =>
        {
            Dispatcher.InvokeAsync(() => _middleClickOverlay?.HideOverlay());
        };
        _middleClickEngine.DirectionChanged += (nx, ny) =>
        {
            _middleClickOverlay?.UpdateDirection(nx, ny);
        };

        _hook = new GlobalMouseHook();
        _hook.ShiftKeyHorizontal = _settings.ShiftKeyHorizontal;

        _hook.MouseWheel += (_, args) =>
        {
            if (!_settings.Enabled) return;

            var processName = CachedProcessHelper.GetProcessUnderCursor();
            if (_settings.IsExcluded(processName)) return;

            args.Handled = true;
            _engine!.OnWheel(args.Delta);
        };
        _hook.MouseHWheel += (_, args) =>
        {
            if (!_settings.Enabled) return;

            var processName = CachedProcessHelper.GetProcessUnderCursor();
            if (_settings.IsExcluded(processName)) return;

            args.Handled = true;
            _engine!.OnHWheel(args.Delta);
        };
        _hook.MouseZoomWheel += (_, args) =>
        {
            if (!_settings.Enabled || !_settings.ZoomSmoothing) return;

            var processName = CachedProcessHelper.GetProcessUnderCursor();
            if (_settings.IsExcluded(processName)) return;

            args.Handled = true;
            _zoomEngine!.OnZoom(args.Delta);
        };
        _hook.MiddleButtonDown += (_, args) =>
        {
            if (!_settings.Enabled || !_settings.MiddleClickScroll) return;
            _middleClickEngine!.OnMiddleDown(args.X, args.Y);
        };
        _hook.MiddleButtonUp += (_, _) =>
        {
            if (!_settings.MiddleClickScroll) return;
            _middleClickEngine!.OnMiddleUp();
        };
        _hook.MouseMoved += (_, args) =>
        {
            _middleClickEngine?.OnMouseMove(args.X, args.Y);
        };

        UpdateHookState();

        bool shouldStartMinimized = _settings.StartWithWindows && _settings.StartMinimized;
        
        if (!shouldStartMinimized)
        {
            ShowSettingsWindow();
        }
        else
        {
            Log.Information("Starting minimized to system tray");
        }
        
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
            _zoomEngine?.Start();
            _middleClickEngine?.Start();
            _hook.Install();
        }
        else
        {
            _hook.Uninstall();
            _engine.Stop();
            _zoomEngine?.Stop();
            _middleClickEngine?.Stop();
            _middleClickEngine?.OnMiddleUp();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _hook?.Dispose();
        _engine?.Dispose();
        _zoomEngine?.Dispose();
        _middleClickEngine?.Dispose();
        _tray?.Dispose();
        LoggingConfig.Shutdown();
    }

    private void ShowSettingsWindow()
    {
        if (_vm is null) return;
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(_vm);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
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

