using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Serilog;
using SoftScroll.Native;

namespace SoftScroll.Core;

    /// <summary>
    /// Detects and tracks input devices (touchpad vs mouse) to enable
    /// automatic smooth scroll disable when using the laptop touchpad.
    /// Uses Raw Input API for device identification via RID_DEVICE_INFO.
    /// </summary>
    public sealed class InputDeviceDetector : IDisposable
    {
        private readonly object _lock = new();
        private bool _lastEventFromTouchpad;
        private IntPtr _lastEventDeviceHandle = IntPtr.Zero;
        private DateTime _lastEventTime = DateTime.MinValue;
        private bool _disposed;

        // Track known touchpad device handles - detected by fTouchPad flag
        private readonly HashSet<IntPtr> _knownTouchpadHandles = new();
        // Track known mouse device handles
        private readonly HashSet<IntPtr> _knownMouseHandles = new();

        // Timeout in milliseconds - if no raw input received within this time,
        // assume we're dealing with an external mouse
        private const int DEVICE_TIMEOUT_MS = 1000;

    public event EventHandler<bool>? DeviceTypeChanged;

    /// <summary>
    /// True if the last input event came from a touchpad.
    /// </summary>
    public bool LastEventFromTouchpad => _lastEventFromTouchpad;

    /// <summary>
    /// Enumerates all connected input devices and identifies touchpads using
    /// the fTouchPad flag from RID_DEVICE_INFO_MOUSE structure.
    /// This is the most reliable way to detect touchpads on Windows.
    /// </summary>
    public void EnumerateDevices()
    {
        lock (_lock)
        {
            try
            {
                uint deviceCount = 0;
                uint result = NativeMethods.GetRawInputDeviceList(null, ref deviceCount, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICELIST>());

                if (result != 0 || deviceCount == 0)
                {
                    Log.Warning("[InputDetector] Failed to get device count: {Result}", result);
                    return;
                }

                var devices = new NativeMethods.RAWINPUTDEVICELIST[deviceCount];
                result = NativeMethods.GetRawInputDeviceList(devices, ref deviceCount, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICELIST>());

                if (result != 0)
                {
                    Log.Warning("[InputDetector] Failed to enumerate devices: {Result}", result);
                    return;
                }

                _knownTouchpadHandles.Clear();
                _knownMouseHandles.Clear();

                foreach (var device in devices)
                {
                    if (device.dwType == NativeMethods.RIM_TYPEMOUSE)
                    {
                        // Use RID_DEVICE_INFO to get fTouchPad flag - the most reliable method
                        if (IsTouchpadDevice(device.hDevice))
                        {
                            _knownTouchpadHandles.Add(device.hDevice);
                            Log.Information("[InputDetector] Found touchpad device: {Handle}", device.hDevice);
                        }
                        else
                        {
                            _knownMouseHandles.Add(device.hDevice);
                            Log.Debug("[InputDetector] Found mouse device: {Handle}", device.hDevice);
                        }
                    }
                    else if (device.dwType == NativeMethods.RIM_TYPEHID)
                    {
                        // HID devices - check if they're touchpads
                        if (IsTouchpadHidDevice(device.hDevice))
                        {
                            _knownTouchpadHandles.Add(device.hDevice);
                            Log.Information("[InputDetector] Found touchpad HID device: {Handle}", device.hDevice);
                        }
                    }
                }

                Log.Information("[InputDetector] Enumerated {Count} devices, found {TouchpadCount} touchpad(s), {MouseCount} mouse(s)",
                    deviceCount, _knownTouchpadHandles.Count, _knownMouseHandles.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[InputDetector] Error during device enumeration");
            }
        }
    }

    /// <summary>
    /// Checks if a mouse device is a touchpad using the fTouchPad flag from RID_DEVICE_INFO.
    /// This is the official Windows way to detect touchpads.
    /// </summary>
    private static bool IsTouchpadDevice(IntPtr hDevice)
    {
        try
        {
            uint infoSize = 0;
            // First call to get the size needed
            uint result = NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref infoSize);
            if (infoSize == 0) return false;

            // Allocate and fill the info structure
            IntPtr infoPtr = Marshal.AllocHGlobal((int)infoSize);
            try
            {
                var info = new NativeMethods.RID_DEVICE_INFO
                {
                    cbSize = infoSize
                };
                Marshal.StructureToPtr(info, infoPtr, false);

                result = NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, infoPtr, ref infoSize);
                if (result == unchecked((uint)-1)) return false;

                var deviceInfo = Marshal.PtrToStructure<NativeMethods.RID_DEVICE_INFO>(infoPtr);

                if (deviceInfo.dwType == NativeMethods.RIM_TYPEMOUSE)
                {
                    bool isTouchpad = deviceInfo.info.mouse.fTouchPad != 0;
                    Log.Debug("[InputDetector] Device {Handle} fTouchPad={IsTouchpad}", hDevice, isTouchpad);
                    return isTouchpad;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(infoPtr);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[InputDetector] Error checking device type via RID_DEVICE_INFO");
        }
        return false;
    }

    /// <summary>
    /// Checks if a HID device is a touchpad by examining its capabilities.
    /// </summary>
    private static bool IsTouchpadHidDevice(IntPtr hDevice)
    {
        try
        {
            // Try to get device name for HID touchpad detection
            uint size = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if (size == 0) return false;

            var buffer = new char[size];
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, buffer, ref size);
            string deviceName = new string(buffer, 0, (int)size).TrimEnd('\0').ToLowerInvariant();

            // HID touchpad patterns - devices with specific prefixes that indicate touchpad
            string[] touchpadPrefixes = { "hid\\", "\\??\\hid" };
            foreach (var prefix in touchpadPrefixes)
            {
                if (deviceName.StartsWith(prefix))
                {
                    // Additional check: if device name contains touch-related keywords
                    if (deviceName.Contains("touch") || deviceName.Contains("pad"))
                        return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "[InputDetector] Error checking HID device name");
        }
        return false;
    }

    /// <summary>
    /// Called from the Raw Input listener when we receive raw input.
    /// This is the primary mechanism for device detection.
    /// </summary>
    public void ProcessRawInput(IntPtr hDevice, uint deviceType)
    {
        lock (_lock)
        {
            try
            {
                // Update the last event device and time
                _lastEventDeviceHandle = hDevice;
                _lastEventTime = DateTime.UtcNow;

                // Check if this device is a known touchpad
                bool isTouchpad = _knownTouchpadHandles.Contains(hDevice);

                if (isTouchpad != _lastEventFromTouchpad)
                {
                    _lastEventFromTouchpad = isTouchpad;
                    Log.Information("[InputDetector] Device changed: {Type}", isTouchpad ? "touchpad" : "mouse/external");
                    DeviceTypeChanged?.Invoke(this, isTouchpad);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[InputDetector] Error processing raw input");
            }
        }
    }

    /// <summary>
    /// Called when any mouse movement occurs - updates device state tracking.
    /// </summary>
    public void OnMouseMove(IntPtr hDevice)
    {
        // Mouse movement also counts as device activity
        // This helps track which device is currently in use
        ProcessRawInput(hDevice, NativeMethods.RIM_TYPEMOUSE);
    }

    /// <summary>
    /// Fallback check for when raw input correlation fails.
    /// Uses a simple timeout-based approach.
    /// </summary>
    public void CheckDeviceState()
    {
        lock (_lock)
        {
            // If we haven't received any raw input recently, assume external mouse
            if ((DateTime.UtcNow - _lastEventTime).TotalMilliseconds > DEVICE_TIMEOUT_MS)
            {
                // No recent input - assume it's safe to use smooth scroll
                // This handles the case where an external mouse was connected
                if (_lastEventFromTouchpad)
                {
                    _lastEventFromTouchpad = false;
                    Log.Information("[InputDetector] Timeout - reverting to external mouse mode");
                    DeviceTypeChanged?.Invoke(this, false);
                }
            }
        }
    }

    /// <summary>
    /// Determines if smooth scroll should be disabled based on current device.
    /// </summary>
    public bool ShouldDisableSmoothScroll()
    {
        lock (_lock)
        {
            return _lastEventFromTouchpad;
        }
    }

    /// <summary>
    /// Called when a scroll event occurs to perform fallback checks.
    /// </summary>
    public void OnScrollEvent()
    {
        CheckDeviceState();
    }

    /// <summary>
    /// Resets the device state (call when focus changes).
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _lastEventFromTouchpad = false;
            _lastEventDeviceHandle = IntPtr.Zero;
            _lastEventTime = DateTime.MinValue;
        }
    }

    public int TouchpadCount { get { lock (_lock) return _knownTouchpadHandles.Count; } }

    public int MouseCount { get { lock (_lock) return _knownMouseHandles.Count; } }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
