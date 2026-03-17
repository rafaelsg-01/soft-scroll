using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SoftScroll;

public sealed class ZoomSmoothEngine : IDisposable
{
    private readonly object _lock = new();
    private Thread? _thread;
    private volatile bool _running;
    private double _remainingDelta;
    private double _unitAccum;

    private const int ZOOM_DURATION_MS = 150;
    private const double FRAME_MS = ScrollConstants.FRAME_MS;
    private const int WHEEL_DELTA = ScrollConstants.WHEEL_DELTA;

    public void Start()
    {
        lock (_lock)
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Worker) { IsBackground = true, Name = "ZoomSmoothEngine" };
            _thread.Start();
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _running = false;
            _remainingDelta = 0;
            _unitAccum = 0;
        }
        _thread?.Join(1000);
    }

    public void OnZoom(int delta)
    {
        lock (_lock)
        {
            _remainingDelta += delta;
        }
    }

    private void Worker()
    {
        var sw = Stopwatch.StartNew();
        double lastMs = sw.Elapsed.TotalMilliseconds;

        while (_running)
        {
            var nowMs = sw.Elapsed.TotalMilliseconds;
            var dt = Math.Max(1.0, nowMs - lastMs);
            lastMs = nowMs;

            int output;
            lock (_lock)
            {
                output = Step(dt);
            }

            if (output != 0) SendCtrlWheel(output);

            var sleep = FRAME_MS - (sw.Elapsed.TotalMilliseconds - nowMs);
            if (sleep > 0) Thread.Sleep((int)Math.Round(sleep));
            else Thread.SpinWait(ScrollConstants.SPIN_WAIT_COUNT);
        }
    }

    private int Step(double dtMs)
    {
        if (Math.Abs(_remainingDelta) < 0.1)
        {
            _remainingDelta = 0;
            _unitAccum = 0;
            return 0;
        }

        var t = Math.Min(1.0, dtMs / ZOOM_DURATION_MS);
        var frac = 1.0 - Math.Pow(1.0 - t, 3); // CubicOut

        var emit = _remainingDelta * frac;
        _remainingDelta -= emit;

        _unitAccum += emit / WHEEL_DELTA;

        int pulses = 0;
        if (Math.Abs(_unitAccum) >= 1.0)
        {
            pulses = (int)_unitAccum;
            _unitAccum -= pulses;
        }

        if (pulses == 0) return 0;
        return Math.Clamp(pulses, -5, 5) * WHEEL_DELTA;
    }

    private static void SendCtrlWheel(int mouseData)
    {
        var inputs = new INPUT[]
        {
            new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL } } },
            new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_WHEEL, mouseData = mouseData } } },
            new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP } } },
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public void Dispose() => Stop();

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;
    private const int MOUSEEVENTF_WHEEL = 0x0800;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx; public int dy; public int mouseData; public int dwFlags; public int time; public nint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public int dwFlags; public int time; public nint dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
