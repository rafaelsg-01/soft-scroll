using System;
using System.Windows;
using Serilog;
using SoftScroll.Core;
using SoftScroll.Hooks;
using SoftScroll.Native;
using SoftScroll.Settings;
using SoftScroll.Infrastructure;
using SoftScroll.UI;

namespace SoftScroll;

public partial class App : System.Windows.Application
{
    private TrayIcon? _tray;
    private GlobalMouseHook? _hook;
    private GlobalHotkey? _hotkey;
    private SettingsViewModel? _vm;
    private SmoothScrollEngine? _engine;
    private ZoomSmoothEngine? _zoomEngine;
    private MiddleClickScrollEngine? _middleClickEngine;
    private MiddleClickOverlay? _middleClickOverlay;
    private ScrollIndicator? _scrollIndicator;
    private SettingsWindow? _settingsWindow;
    private AppSettings _settings = null!;

    // Debounced exclusion: check process name every 50 ms instead of every wheel event
    private string? _lastExcludedProcess;
    private long _lastExcludedCheck;
    private const long EXCLUSION_CHECK_MS = 50;

    protected override void OnStartup(StartupEventArgs e)
    {
        LoggingConfig.Configure();

        NativeMethods.timeBeginPeriod(1);

        base.OnStartup(e);

        _settings = AppSettings.Load();
        _vm = new SettingsViewModel(_settings);
        _vm.SettingsChanged += (_, _) =>
        {
            _tray?.UpdateEnabled(_vm.Enabled);
            _tray?.RefreshLocalization();
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
        _tray.ToggleHotkeyRequested += (_, _) => ToggleEnabled();

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
        _middleClickEngine.DirectionChanged += (nx, ny, magnitude) =>
        {
            _middleClickOverlay?.UpdateDirection(nx, ny, magnitude);
        };

        _hook = new GlobalMouseHook();
        _hook.ShiftKeyHorizontal = _settings.ShiftKeyHorizontal;

        // Setup hotkey for quick toggle (Ctrl+Alt+S)
        if (_settings.EnableGlobalHotkey)
        {
            _hotkey = new GlobalHotkey(
                id: 1,
                modifiers: HotkeyConstants.MOD_CONTROL | HotkeyConstants.MOD_ALT | HotkeyConstants.MOD_NOREPEAT,
                key: HotkeyConstants.VK_S
            );
            _hotkey.HotkeyPressed += (_, _) => ToggleEnabled();
            _hotkey.Install();
        }

        _hook.MouseWheel += (_, args) =>
        {
            if (!_settings.Enabled) return;
            if (IsExcludedApp()) return;

            // Show scroll indicator if enabled
            if (_settings.ShowScrollIndicator)
            {
                ShowScrollIndicator(args.Delta);
            }

            // Check for app-specific profile
            var profile = _settings.GetAppProfile(_lastExcludedProcess);
            if (profile != null && profile.Enabled)
            {
                // Apply app profile settings temporarily
                _engine!.OnWheelWithSettings(args.Delta, profile.ToAppSettings());
            }
            else
            {
                args.Handled = true;
                _engine!.OnWheel(args.Delta);
                ScrollStatistics.Instance.RecordScroll(args.Delta);
            }
        };
        _hook.MouseHWheel += (_, args) =>
        {
            if (!_settings.Enabled) return;
            if (IsExcludedApp()) return;

            args.Handled = true;
            _engine!.OnHWheel(args.Delta);
            ScrollStatistics.Instance.RecordScroll(args.Delta);
        };
        _hook.MouseZoomWheel += (_, args) =>
        {
            if (!_settings.Enabled || !_settings.ZoomSmoothing) return;
            if (IsExcludedApp()) return;

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

    private void ToggleEnabled()
    {
        var newState = !_settings.Enabled;
        _settings.Enabled = newState;
        _settings.Save();
        _vm!.Enabled = newState;
        _tray?.UpdateEnabled(newState);
        UpdateHookState();
        Log.Information("Soft Scroll {State}", newState ? "enabled" : "disabled");
    }

    private bool IsExcludedApp()
    {
        var now = Environment.TickCount64;
        if (now - _lastExcludedCheck > EXCLUSION_CHECK_MS)
        {
            _lastExcludedProcess = CachedProcessHelper.GetProcessUnderCursor();
            _lastExcludedCheck = now;
        }
        return _settings.IsExcluded(_lastExcludedProcess);
    }

    private void ShowScrollIndicator(int speed)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _scrollIndicator ??= new ScrollIndicator();
            var pos = GetCursorPosition();
            _scrollIndicator.UpdateSpeed(speed);
            if (!_scrollIndicator.IsVisible)
            {
                _scrollIndicator.ShowAt(pos.X, pos.Y, speed);
            }
        });
    }

    private System.Drawing.Point GetCursorPosition()
    {
        NativeMethods.GetCursorPos(out var point);
        return new System.Drawing.Point(point.x, point.y);
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
        _hotkey?.Dispose();
        _engine?.Dispose();
        _zoomEngine?.Dispose();
        _middleClickEngine?.Dispose();
        _tray?.Dispose();
        LoggingConfig.Shutdown();
        NativeMethods.timeEndPeriod(1);
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

