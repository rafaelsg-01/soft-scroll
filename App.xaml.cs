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
    private InputDeviceDetector? _inputDetector;
    private RawInputListener? _rawInputListener;
    private AppSettings _settings = null!;

    // Static event for device state changes - used by SettingsWindow to update UI
    public static event EventHandler<DeviceStateEventArgs>? DeviceStateChanged;

    // Debounced exclusion: check process name every 50 ms instead of every wheel event
    private readonly object _exclusionLock = new();
    private string? _lastExcludedProcess;
    private long _lastExcludedCheck;
    private const long EXCLUSION_CHECK_MS = 50;

    // Disable-while-held: track modifier keys and middle button state
    private readonly KeyboardStateSampler _disableKeysSampler = new();
    private volatile bool _middleButtonHeld;
    private bool _disableWhileHoldingInitialized;

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

        // Initialize touchpad detection
        _inputDetector = new InputDeviceDetector();

        // Subscribe to device type changes to update UI
        _inputDetector.DeviceTypeChanged += (_, isTouchpad) =>
        {
            Dispatcher.InvokeAsync(() => {
                UpdateDeviceStateUI(isTouchpad, _inputDetector.TouchpadCount, _inputDetector.MouseCount);
                _tray?.UpdateTouchpadState(isTouchpad);
            });
        };

        // Initialize Raw Input listener for device tracking
        _rawInputListener = new RawInputListener(_inputDetector);

        // Subscribe to devices changed event for re-enumeration
        _rawInputListener.DevicesChanged += (_, _) =>
        {
            Dispatcher.InvokeAsync(() => UpdateDeviceStateUI(_inputDetector.ShouldDisableSmoothScroll(), _inputDetector.TouchpadCount, _inputDetector.MouseCount));
        };

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
            if (IsDisabledByHold()) return;
            if (IsExcludedApp()) return;
            if (IsOwnWindow()) return;

            // Auto-disable on touchpad if setting is enabled
            if (_settings.AutoDisableOnTouchpad)
            {
                // First check device state (fallback)
                _inputDetector?.OnScrollEvent();
                if (_inputDetector?.ShouldDisableSmoothScroll() == true)
                    return;
            }

            // Show scroll indicator if enabled
            if (_settings.ShowScrollIndicator)
            {
                ShowScrollIndicator(args.Delta);
            }

            // Check for app-specific profile
            string? procName;
            lock (_exclusionLock) { procName = _lastExcludedProcess; }
            var profile = _settings.GetAppProfile(procName ?? "");
            if (profile != null && profile.Enabled)
            {
                _lastScrollWasHorizontal = false;
                // Apply app profile settings temporarily
                _engine!.OnWheelWithSettings(args.Delta, profile.ToAppSettings());
            }
            else
            {
                ResetHorizontalCarryIfNeeded();
                args.Handled = true;
                _engine!.OnWheel(args.Delta);
                ScrollStatistics.Instance.RecordScroll(args.Delta);
            }
        };
        _hook.MouseHWheel += (_, args) =>
        {
            if (!_settings.Enabled) return;
            if (IsDisabledByHold()) return;
            if (IsExcludedApp()) return;
            if (IsOwnWindow()) return;

            // Auto-disable on touchpad if setting is enabled
            if (_settings.AutoDisableOnTouchpad)
            {
                // First check device state (fallback)
                _inputDetector?.OnScrollEvent();
                if (_inputDetector?.ShouldDisableSmoothScroll() == true)
                    return;
            }

            args.Handled = true;
            _lastScrollWasHorizontal = true;
            _engine!.OnHWheel(args.Delta);
            ScrollStatistics.Instance.RecordScroll(args.Delta);
        };
        _hook.MouseZoomWheel += (_, args) =>
        {
            if (!_settings.Enabled || !_settings.ZoomSmoothing) return;
            if (IsDisabledByHold()) return;
            if (IsExcludedApp()) return;
            if (IsOwnWindow()) return;

            args.Handled = true;
            _lastScrollWasHorizontal = false;
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
            ShowSettingsWindow(show: true, startMinimized: false);
        }
        else
        {
            // Show tray icon but don't show the settings window
            ShowSettingsWindow(show: false, startMinimized: true);
            Log.Information("Starting minimized to system tray");
        }

        // Ensure Raw Input listener initializes on the UI thread after window creation
        if (_settings.AutoDisableOnTouchpad)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (_settingsWindow != null)
                    _rawInputListener?.Initialize(_settingsWindow);
            });
        }

        // Enumerate devices after Raw Input is registered
        Dispatcher.InvokeAsync(() => _inputDetector?.EnumerateDevices());

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

    private bool _lastScrollWasHorizontal;

    private void ResetHorizontalCarryIfNeeded()
    {
        if (!_lastScrollWasHorizontal)
            return;

        _engine?.ResetHorizontalAxis();
        _lastScrollWasHorizontal = false;
    }
    private const long OWN_WINDOW_CHECK_MS = 50;
    private readonly int _ownProcessId = Environment.ProcessId;
    private bool _isOwnWindow;
    private IntPtr _lastForegroundWindow;
    private long _lastOwnWindowCheck;

    private bool IsOwnWindow()
    {
        var now = Environment.TickCount64;
        if (now - _lastOwnWindowCheck <= OWN_WINDOW_CHECK_MS)
            return _isOwnWindow;
        _lastOwnWindowCheck = now;

        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == _lastForegroundWindow)
            return _isOwnWindow;

        _lastForegroundWindow = hwnd;
        if (hwnd == IntPtr.Zero)
        {
            _isOwnWindow = false;
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        _isOwnWindow = pid == (uint)_ownProcessId;
        return _isOwnWindow;
    }

    private bool IsExcludedApp()
    {
        var now = Environment.TickCount64;
        if (now - _lastExcludedCheck > EXCLUSION_CHECK_MS)
        {
            _lastExcludedProcess = CachedProcessHelper.GetProcessUnderCursor();
            _lastExcludedCheck = now;
        }
        if (_settings.IsWhitelistMode)
            return !_settings.IsWhitelisted(_lastExcludedProcess);
        return _settings.IsExcluded(_lastExcludedProcess);
    }

    private void InitializeDisableWhileHolding()
    {
        if (_disableWhileHoldingInitialized) return;
        _disableWhileHoldingInitialized = true;

        var hasKeys = _settings.DisableWhileHoldingKeys.Count > 0;
        var hasMiddle = _settings.DisableWhileHoldingMiddleButton;
        if (!hasKeys && !hasMiddle) return;

        _disableKeysSampler.Start();

        if (hasMiddle)
        {
            _hook!.MiddleButtonDown += (_, _) => { _middleButtonHeld = true; };
            _hook!.MiddleButtonUp += (_, _) => { _middleButtonHeld = false; };
        }
    }

    private bool IsDisabledByHold()
    {
        if (!_disableWhileHoldingInitialized) return false;

        foreach (var key in _settings.DisableWhileHoldingKeys)
        {
            var vk = key.ToUpperInvariant();
            if (vk == "CTRL" && _disableKeysSampler.IsCtrlPressed) return true;
            if (vk == "SHIFT" && _disableKeysSampler.IsShiftPressed) return true;
            if (vk == "ALT" && _disableKeysSampler.IsAltPressed) return true;
            if (vk == "WIN" || vk == "WINDOWS")
            {
                // Win key: check VK_LWIN or VK_RWIN via GetAsyncKeyState
                if ((NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
                    (NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0)
                    return true;
            }
        }
        if (_settings.DisableWhileHoldingMiddleButton && _middleButtonHeld) return true;
        return false;
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
            InitializeDisableWhileHolding();
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
        _inputDetector?.Dispose();
        _rawInputListener?.Dispose();
        LoggingConfig.Shutdown();
        NativeMethods.timeEndPeriod(1);
    }

    private void ShowSettingsWindow(bool show = true, bool startMinimized = false)
    {
        if (_vm is null) return;

        // Don't show the window at all when minimized
        if (!show) return;

        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(_vm);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Owner = null;

            // Apply minimized state before showing if needed
            if (startMinimized)
            {
                _settingsWindow.WindowState = WindowState.Minimized;
                _settingsWindow.Show();
                // Ensure it doesn't flash on taskbar
                _settingsWindow.ShowInTaskbar = false;
            }
            else
            {
                _settingsWindow.Show();
            }
        }
        else
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.Show();
            _settingsWindow.ShowInTaskbar = true;
            _settingsWindow.Activate();
        }
    }

    /// <summary>
    /// Called to update the UI when device state changes
    /// </summary>
    internal void UpdateDeviceStateUI(bool isTouchpad, int touchpadCount, int mouseCount)
    {
        DeviceStateChanged?.Invoke(this, new DeviceStateEventArgs(isTouchpad, touchpadCount, mouseCount));
    }
}

public class DeviceStateEventArgs : EventArgs
{
    public bool IsTouchpadActive { get; }
    public int TouchpadCount { get; }
    public int MouseCount { get; }

    public DeviceStateEventArgs(bool isTouchpadActive, int touchpadCount, int mouseCount)
    {
        IsTouchpadActive = isTouchpadActive;
        TouchpadCount = touchpadCount;
        MouseCount = mouseCount;
    }
}

