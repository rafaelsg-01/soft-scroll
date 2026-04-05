using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Serilog;
using SoftScroll.Native;

namespace SoftScroll.Core;

/// <summary>
/// Listens for Raw Input messages to detect which input device (touchpad vs mouse)
/// is currently being used. Also listens for device change events to re-enumerate devices.
/// Uses a hidden message-only window.
/// </summary>
public sealed class RawInputListener : IDisposable
{
    private static readonly int WM_INPUT = NativeMethods.WM_INPUT;
    private const int WM_DEVICECHANGE = 0x0219;

    private readonly InputDeviceDetector _deviceDetector;
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _disposed;
    private bool _registered;

    // Throttle: minimum time between raw input processing (ms)
    // Increased to aggressively reduce UI thread load from mouse movement
    private const int THROTTLE_MS = 500;
    private long _lastProcessTick;

    // MUST keep delegate reference alive to prevent GC from collecting it
    private WndProcDelegate? _myWndProcDelegate;

    public event EventHandler? DevicesChanged;

    public RawInputListener(InputDeviceDetector deviceDetector)
    {
        _deviceDetector = deviceDetector ?? throw new ArgumentNullException(nameof(deviceDetector));
    }

    /// <summary>
    /// Initializes the Raw Input listener with a hidden window.
    /// Call this after the main window handle is available.
    /// </summary>
    public void Initialize(Window ownerWindow)
    {
        if (_registered)
            return;

        try
        {
            // Create a hidden window for Raw Input
            _hwnd = CreateHiddenWindow();
            if (_hwnd == IntPtr.Zero)
            {
                Log.Warning("[RawInput] Failed to create hidden window");
                return;
            }

            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            // Register for Raw Input from mice
            if (!RegisterRawInputDevices())
            {
                Log.Warning("[RawInput] Failed to register Raw Input devices");
            }
            else
            {
                _registered = true;
                Log.Information("[RawInput] Registered for Raw Input");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RawInput] Failed to initialize Raw Input listener");
        }
    }

    private IntPtr CreateHiddenWindow()
    {
        // CRITICAL: Keep delegate alive to prevent GC
        _myWndProcDelegate = MyWndProc;

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = _myWndProcDelegate,
            hInstance = GetModuleHandle(null),
            lpszClassName = "SoftScroll_RawInput_Window"
        };

        uint classAtom = RegisterClassEx(ref wc);
        if (classAtom == 0)
        {
            Log.Warning("[RawInput] RegisterClassEx failed: {Error}", Marshal.GetLastWin32Error());
            return IntPtr.Zero;
        }

        // Create message-only window
        _hwnd = CreateWindowEx(
            0,
            new IntPtr(classAtom),
            "SoftScroll RawInput",
            0,
            0, 0, 0, 0,
            IntPtr.Zero,
            IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            Log.Warning("[RawInput] CreateWindowEx failed: {Error}", Marshal.GetLastWin32Error());
        }

        return _hwnd;
    }

    private bool RegisterRawInputDevices()
    {
        var rid = new NativeMethods.RAWINPUTDEVICE[1];
        rid[0].usUsagePage = 0x01;
        rid[0].usUsage = 0x02;
        rid[0].dwFlags = 0x00000100; // RIDEV_INPUTSINK
        rid[0].hwndTarget = _hwnd;

        bool result = NativeMethods.RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>());
        if (!result)
        {
            Log.Warning("[RawInput] RegisterRawInputDevices failed: {Error}", Marshal.GetLastWin32Error());
            return false;
        }

        return true;
    }

    private IntPtr MyWndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_INPUT)
        {
            ProcessRawInputThrottled(lParam);
            return IntPtr.Zero;
        }
        else if (msg == WM_DEVICECHANGE)
        {
            OnDeviceChange(wParam);
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            ProcessRawInputThrottled(lParam);
            handled = true;
            return IntPtr.Zero;
        }
        else if (msg == WM_DEVICECHANGE)
        {
            OnDeviceChange(wParam);
            handled = true;
            return IntPtr.Zero;
        }
        handled = false;
        return IntPtr.Zero;
    }

    private void ProcessRawInputThrottled(IntPtr lParam)
    {
        var now = Environment.TickCount64;
        if (now - _lastProcessTick < THROTTLE_MS)
            return;
        _lastProcessTick = now;

        try
        {
            uint size = 0;
            uint result = NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());

            if (size == 0 || result == unchecked((uint)-1))
                return;

            // Reuse a static buffer to avoid frequent AllocHGlobal calls
            IntPtr data = GetOrCreateBuffer((int)size);
            if (data == IntPtr.Zero) return;

            result = NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, data, ref size, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());
            if (result == unchecked((uint)-1) || result == 0)
                return;

            var header = Marshal.PtrToStructure<NativeMethods.RAWINPUTHEADER>(data);

            if (header.dwType == NativeMethods.RIM_TYPEMOUSE)
            {
                IntPtr mouseDataPtr = data + Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>();
                var mouseData = Marshal.PtrToStructure<NativeMethods.RAWINPUTMOUSE>(mouseDataPtr);

                // Only process button/wheel events, SKIP mousemove (usFlags bit 0x01)
                if ((mouseData.usFlags & 0x01) == 0)
                {
                    _deviceDetector.ProcessRawInput(header.hDevice, header.dwType);
                }
            }
            else if (header.dwType == NativeMethods.RIM_TYPEHID)
            {
                _deviceDetector.ProcessRawInput(header.hDevice, header.dwType);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[RawInput] Error processing raw input");
        }
    }

    // Reusable buffer to avoid Marshal.AllocHGlobal on every call
    private IntPtr _buffer;
    private int _bufferSize;

    private IntPtr GetOrCreateBuffer(int size)
    {
        if (_buffer == IntPtr.Zero || size > _bufferSize)
        {
            if (_buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(_buffer);
            _buffer = Marshal.AllocHGlobal(size);
            _bufferSize = size;
        }
        return _buffer;
    }

    private void OnDeviceChange(IntPtr wParam)
    {
        int eventType = wParam.ToInt32();
        if (eventType == 0x8000 || eventType == 0x8004)
        {
            Log.Information("[RawInput] Device change detected: {Event}", eventType == 0x8000 ? "connected" : "disconnected");

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    _deviceDetector.EnumerateDevices();
                    DevicesChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[RawInput] Error re-enumerating devices");
                }
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            // Free the reusable buffer
            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = IntPtr.Zero;
                _bufferSize = 0;
            }

            // Release delegate so window class can be cleaned up
            _myWndProcDelegate = null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[RawInput] Error during disposal");
        }

        GC.SuppressFinalize(this);
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint RegisterClassEx(ref WNDCLASSEX lpWndClass);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        IntPtr lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public WndProcDelegate? lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string? lpszClassName;
        public IntPtr hIconSm;
    }
}
