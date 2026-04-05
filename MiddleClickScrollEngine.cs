using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SoftScroll;

public sealed class MiddleClickScrollEngine : IDisposable
{
    private readonly object _lock = new();
    private Thread? _thread;
    private volatile bool _running;
    private readonly ManualResetEventSlim _signal = new(false);
    private volatile bool _active;

    private int _originX, _originY;
    private int _currentX, _currentY;
    private int _deadZone = 10;

    private static readonly double FRAME_MS = ScrollConstants.FRAME_MS;
    private static readonly int WHEEL_DELTA = ScrollConstants.WHEEL_DELTA;

    public event Action<int, int>? Activated;   // (originX, originY)
    public event Action? Deactivated;
    public event Action<double, double, double>? DirectionChanged; // (normalizedX, normalizedY, magnitude) for overlay

    public void UpdateDeadZone(int px) { lock (_lock) _deadZone = px; }

    public void Start()
    {
        lock (_lock)
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Worker) { IsBackground = true, Name = "MiddleClickScroll" };
            _thread.Start();
        }
    }

    public void Stop()
    {
        lock (_lock) { _running = false; _active = false; }
        _signal.Set();
        _thread?.Join(1000);
    }

    public void OnMiddleDown(int x, int y)
    {
        lock (_lock)
        {
            _originX = x;
            _originY = y;
            _currentX = x;
            _currentY = y;
            _active = true;
        }
        Activated?.Invoke(x, y);
        _signal.Set();
    }

    public void OnMiddleUp()
    {
        lock (_lock) { _active = false; }
        Deactivated?.Invoke();
        _signal.Set();
    }

    public void OnMouseMove(int x, int y)
    {
        if (!_active) return;
        lock (_lock)
        {
            _currentX = x;
            _currentY = y;
        }
    }

    private void Worker()
    {
        var sw = Stopwatch.StartNew();
        double lastMs = sw.Elapsed.TotalMilliseconds;
        double accumV = 0, accumH = 0;

        while (_running)
        {
            try
            {
                if (!_active)
                {
                    _signal.Wait(TimeSpan.FromMilliseconds(100));
                    _signal.Reset();
                    // Reset time base after idle to prevent frame-1 jitter
                    lastMs = sw.Elapsed.TotalMilliseconds;
                    accumV = 0;
                    accumH = 0;
                    continue;
                }

                var nowMs = sw.Elapsed.TotalMilliseconds;
                var dt = Math.Max(1.0, nowMs - lastMs);
                lastMs = nowMs;

                int dx, dy, dz;
                lock (_lock)
                {
                    dx = _currentX - _originX;
                    dy = _currentY - _originY;
                    dz = _deadZone;
                }

                // Compute scroll speed from distance (with dead zone)
                double speedV = 0, speedH = 0;
                if (Math.Abs(dy) > dz)
                {
                    var effective = Math.Abs(dy) - dz;
                    speedV = Math.Sign(dy) * SpeedCurve(effective) * dt * 0.01;
                }
                if (Math.Abs(dx) > dz)
                {
                    var effective = Math.Abs(dx) - dz;
                    speedH = Math.Sign(dx) * SpeedCurve(effective) * dt * 0.01;
                }

                // Notify overlay of direction with magnitude
                var maxDist = 200.0;
                var nx = Math.Clamp(dx / maxDist, -1.0, 1.0);
                var ny = Math.Clamp(dy / maxDist, -1.0, 1.0);
                var magnitude = Math.Sqrt(nx * nx + ny * ny);
                DirectionChanged?.Invoke(nx, ny, magnitude);

                accumV += speedV;
                accumH += speedH;

                // Buffered SendInput: emit both axes in a single call
                int vPulses = 0, hPulses = 0;
                if (Math.Abs(accumV) >= 1.0)
                {
                    vPulses = (int)accumV;
                    accumV -= vPulses;
                }
                if (Math.Abs(accumH) >= 1.0)
                {
                    hPulses = (int)accumH;
                    accumH -= hPulses;
                }

                if (vPulses != 0 || hPulses != 0)
                    SendWheel(-vPulses * WHEEL_DELTA, hPulses * WHEEL_DELTA);

                var sleep = FRAME_MS - (sw.Elapsed.TotalMilliseconds - nowMs);
                if (sleep > 0) Thread.Sleep((int)Math.Round(sleep));
                else Thread.SpinWait(ScrollConstants.SPIN_WAIT_COUNT);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MiddleClickScrollEngine worker: {ex.Message}");
            }
        }
    }

    private static double SpeedCurve(double distance)
    {
        // Quadratic speed curve: slow near dead zone, fast at distance
        return Math.Min(distance * distance * 0.001, 30.0);
    }

    private static void SendWheel(int vMouseData, int hMouseData)
    {
        var size = Marshal.SizeOf<NativeMethods.INPUT>();

        if (hMouseData != 0)
        {
            var inputs = new NativeMethods.INPUT[]
            {
                new() { type = 0, U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_WHEEL, mouseData = vMouseData } } },
                new() { type = 0, U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_HWHEEL, mouseData = hMouseData } } },
            };
            NativeMethods.SendInput(2, inputs, size);
        }
        else if (vMouseData != 0)
        {
            var inp = new NativeMethods.INPUT
            {
                type = 0,
                U = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_WHEEL, mouseData = vMouseData }
                }
            };
            NativeMethods.SendInput(1, [inp], size);
        }
    }

    public void Dispose() => Stop();
}
