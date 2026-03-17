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
    private volatile bool _active;

    private int _originX, _originY;
    private int _currentX, _currentY;
    private int _deadZone = 10;

    private static readonly double FRAME_MS = ScrollConstants.FRAME_MS;
    private static readonly int WHEEL_DELTA = ScrollConstants.WHEEL_DELTA;

    public event Action<int, int>? Activated;   // (originX, originY)
    public event Action? Deactivated;
    public event Action<double, double>? DirectionChanged; // (normalizedX, normalizedY) for overlay

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
    }

    public void OnMiddleUp()
    {
        lock (_lock) { _active = false; }
        Deactivated?.Invoke();
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
            var nowMs = sw.Elapsed.TotalMilliseconds;
            var dt = Math.Max(1.0, nowMs - lastMs);
            lastMs = nowMs;

            if (_active)
            {
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

                // Notify overlay of direction
                var maxDist = 200.0;
                DirectionChanged?.Invoke(
                    Math.Clamp(dx / maxDist, -1.0, 1.0),
                    Math.Clamp(dy / maxDist, -1.0, 1.0)
                );

                accumV += speedV;
                accumH += speedH;

                // Emit vertical scroll
                if (Math.Abs(accumV) >= 1.0)
                {
                    int pulses = (int)accumV;
                    accumV -= pulses;
                    // Positive dy = mouse moved down = scroll down (negative wheel delta)
                    SendWheel(-pulses * WHEEL_DELTA);
                }

                // Emit horizontal scroll
                if (Math.Abs(accumH) >= 1.0)
                {
                    int pulses = (int)accumH;
                    accumH -= pulses;
                    SendHWheel(pulses * WHEEL_DELTA);
                }
            }
            else
            {
                accumV = 0;
                accumH = 0;
            }

            var sleep = FRAME_MS - (sw.Elapsed.TotalMilliseconds - nowMs);
            if (sleep > 0) Thread.Sleep((int)Math.Round(sleep));
            else Thread.SpinWait(ScrollConstants.SPIN_WAIT_COUNT);
        }
    }

    private static double SpeedCurve(double distance)
    {
        // Quadratic speed curve: slow near dead zone, fast at distance
        return Math.Min(distance * distance * 0.001, 30.0);
    }

    private static void SendWheel(int mouseData)
    {
        var inp = new INPUT
        {
            type = 0,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_WHEEL, mouseData = mouseData } }
        };
        SendInput(1, [inp], Marshal.SizeOf<INPUT>());
    }

    private static void SendHWheel(int mouseData)
    {
        var inp = new INPUT
        {
            type = 0,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_HWHEEL, mouseData = mouseData } }
        };
        SendInput(1, [inp], Marshal.SizeOf<INPUT>());
    }

    public void Dispose() => Stop();

    private const int MOUSEEVENTF_WHEEL = 0x0800;
    private const int MOUSEEVENTF_HWHEEL = 0x01000;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx; public int dy; public int mouseData; public int dwFlags; public int time; public nint dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
