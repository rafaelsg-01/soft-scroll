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
    private readonly ManualResetEventSlim _signal = new(false);
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
        _signal.Set();
    }

    private void Worker()
    {
        var sw = Stopwatch.StartNew();
        double lastMs = sw.Elapsed.TotalMilliseconds;

        while (_running)
        {
            bool workAvailable;
            lock (_lock)
            {
                workAvailable = Math.Abs(_remainingDelta) >= 0.1;
            }

            if (!workAvailable)
            {
                _signal.Wait(TimeSpan.FromMilliseconds(100));
                _signal.Reset();
                continue;
            }

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
        var inputs = new NativeMethods.INPUT[]
        {
            new() { type = NativeMethods.INPUT_KEYBOARD, U = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = NativeMethods.VK_CONTROL } } },
            new() { type = NativeMethods.INPUT_MOUSE, U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_WHEEL, mouseData = mouseData } } },
            new() { type = NativeMethods.INPUT_KEYBOARD, U = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = NativeMethods.VK_CONTROL, dwFlags = NativeMethods.KEYEVENTF_KEYUP } } },
        };
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    public void Dispose() => Stop();
}
