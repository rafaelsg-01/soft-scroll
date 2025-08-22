using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SmoothScrollClone;

public sealed class SmoothScrollEngine : IDisposable
{
    private readonly object _lock = new();
    private Thread? _thread;
    private bool _running;

    private Axis _v = new();
    private Axis _h = new();

    private AppSettings _s = AppSettings.CreateDefault();

    private const int WHEEL_DELTA = 120;
    private const int EMIT_UNIT = 12; // 10 pulses per notch => smooth
    private const double BASE_STEP_PX = 120.0; // baseline: 120 px -> 1 notch (120 units)

    public SmoothScrollEngine(AppSettings settings) => ApplySettings(settings);

    public void ApplySettings(AppSettings s)
    {
        lock (_lock)
        {
            _s = s;
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Worker) { IsBackground = true, Name = "SmoothScrollEngine" };
            _thread.Start();
        }
    }

    public void Stop()
    {
        lock (_lock) { _running = false; }
        _thread?.Join(1000);
        _thread = null;
        _v = new();
        _h = new();
    }

    public void OnWheel(int delta)
    {
        lock (_lock)
        {
            var dir = _s.ReverseWheelDirection ? -1 : 1;
            var now = Environment.TickCount64;
            _v.RegisterNotch(now, delta * dir, _s);
        }
    }

    public void OnHWheel(int delta)
    {
        lock (_lock)
        {
            var dir = _s.ReverseWheelDirection ? -1 : 1;
            var now = Environment.TickCount64;
            _h.RegisterNotch(now, delta * dir, _s);
        }
    }

    private void Worker()
    {
        var sw = Stopwatch.StartNew();
        const double frameMs = 1000.0 / 120.0; // 120Hz
        double lastMs = sw.Elapsed.TotalMilliseconds;

        while (true)
        {
            bool running;
            lock (_lock) running = _running;
            if (!running) break;

            var nowMs = sw.Elapsed.TotalMilliseconds;
            var dt = Math.Max(1.0, nowMs - lastMs);
            lastMs = nowMs;

            int outV = 0, outH = 0;
            AppSettings s;
            lock (_lock)
            {
                s = _s;
                outV = _v.Step(dt, s);
                if (s.HorizontalSmoothness) outH = _h.Step(dt, s); else outH = 0;
            }

            if (outV != 0) SendWheel(outV);
            if (outH != 0) SendHWheel(outH);

            var sleep = frameMs - (sw.Elapsed.TotalMilliseconds - nowMs);
            if (sleep > 0) Thread.Sleep((int)Math.Round(sleep));
            else Thread.Sleep(1);
        }
    }

    private static void SendWheel(int mouseData)
    {
        var inp = new INPUT
        {
            type = 0,
            U = new InputUnion
            {
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_WHEEL, mouseData = mouseData }
            }
        };
        SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    private static void SendHWheel(int mouseData)
    {
        var inp = new INPUT
        {
            type = 0,
            U = new InputUnion
            {
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_HWHEEL, mouseData = mouseData }
            }
        };
        SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    public void Dispose() => Stop();

    private struct Axis
    {
        public double RemainingPx;   // remaining pixels to emit
        public long LastNotchTime;   // ms
        public int AccelFactor;      // 1..AccelerationMax
        public double UnitAccum;     // accumulated EMIT_UNIT pulses (fractional)

        public void RegisterNotch(long nowMs, int delta, AppSettings s)
        {
            if (nowMs - LastNotchTime <= s.AccelerationDeltaMs)
                AccelFactor = Math.Min(s.AccelerationMax, Math.Max(1, AccelFactor + 1));
            else
                AccelFactor = 1;

            LastNotchTime = nowMs;

            var notches = delta / (double)WHEEL_DELTA; // typically +/-1
            var pixels = notches * s.StepSizePx * AccelFactor;
            RemainingPx += pixels;
        }

        public int Step(double dtMs, AppSettings s)
        {
            if (Math.Abs(RemainingPx) < 0.1)
            {
                RemainingPx = 0;
                UnitAccum = 0;
                return 0;
            }

            var duration = Math.Max(1.0, s.AnimationTimeMs);
            // Exponential ease-out: monotonic decay, less bounce
            var k = s.AnimationEasing ? (2.0 + s.TailToHeadRatio) : 1.0;
            var frac = 1.0 - Math.Exp(-k * (dtMs / duration)); // 0..1 portion to emit this frame

            var emitPx = RemainingPx * frac;
            RemainingPx -= emitPx;

            // Convert emitted pixels to wheel units using a fixed baseline mapping
            var wheelUnits = (emitPx / BASE_STEP_PX) * WHEEL_DELTA; // fractional 120-based units

            // Convert to EMIT_UNIT pulses and accumulate fractional remainder
            var units = wheelUnits / EMIT_UNIT;
            UnitAccum += units;

            int pulses = 0;
            if (Math.Abs(UnitAccum) >= 1.0)
            {
                pulses = (int)UnitAccum; // trunc toward zero
                UnitAccum -= pulses;
            }

            if (pulses == 0) return 0;
            pulses = Math.Clamp(pulses, -20, 20); // limit per frame
            return pulses * EMIT_UNIT;
        }
    }

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
