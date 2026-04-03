using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SoftScroll;

public sealed class SmoothScrollEngine : IDisposable
{
    private readonly object _lock = new();
    private Thread? _thread;
    private volatile bool _running;
    private readonly ManualResetEventSlim _signal = new(false);

    private Axis _v = new();
    private Axis _h = new();

    private AppSettings _s = AppSettings.CreateDefault();

    // Use constants from ScrollConstants
    private static readonly int WHEEL_DELTA = ScrollConstants.WHEEL_DELTA;
    private static readonly int EMIT_UNIT = ScrollConstants.EMIT_UNIT;
    private static readonly double BASE_STEP_PX = ScrollConstants.BASE_STEP_PX;
    private static readonly int PULSE_CLAMP_MIN = ScrollConstants.PULSE_CLAMP_MIN;
    private static readonly int PULSE_CLAMP_MAX = ScrollConstants.PULSE_CLAMP_MAX;
    private static readonly double FRAME_MS = ScrollConstants.FRAME_MS;
    private static readonly int SPIN_WAIT_COUNT = ScrollConstants.SPIN_WAIT_COUNT;

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
        lock (_lock)
        {
            _running = false;
            // Reset axis state inside lock to avoid race with worker thread
            _v = new();
            _h = new();
        }
        _signal.Set();
        _thread?.Join(1000);
    }

    public void OnWheel(int delta)
    {
        lock (_lock)
        {
            var dir = _s.ReverseWheelDirection ? -1 : 1;
            var now = Environment.TickCount64;
            _v.RegisterNotch(now, delta * dir, _s);
        }
        _signal.Set();
    }

    public void OnHWheel(int delta)
    {
        lock (_lock)
        {
            var dir = _s.ReverseWheelDirection ? -1 : 1;
            var now = Environment.TickCount64;
            _h.RegisterNotch(now, delta * dir, _s);
        }
        _signal.Set();
    }

    private void Worker()
    {
        var sw = Stopwatch.StartNew();
        double lastMs = sw.Elapsed.TotalMilliseconds;

        while (_running)
        {
            // Check if there's anything to emit
            bool workAvailable;
            lock (_lock)
            {
                workAvailable = Math.Abs(_v.RemainingPx) >= 0.1
                    || Math.Abs(_h.RemainingPx) >= 0.1;
            }

            if (!workAvailable)
            {
                // Block until a wheel event signals us or timeout elapses.
                // Timeout guarantees eventual shutdown even if no signal arrives.
                _signal.Wait(TimeSpan.FromMilliseconds(100));
                _signal.Reset();
                continue;
            }

            var nowMs = sw.Elapsed.TotalMilliseconds;
            var dt = Math.Max(1.0, nowMs - lastMs);
            lastMs = nowMs;

            int outV = 0, outH = 0;
            lock (_lock)
            {
                outV = _v.Step(dt, _s);
                if (_s.HorizontalSmoothness) outH = _h.Step(dt, _s); else outH = 0;
            }

            if (outV != 0) SendWheel(outV);
            if (outH != 0) SendHWheel(outH);

            var sleep = FRAME_MS - (sw.Elapsed.TotalMilliseconds - nowMs);
            if (sleep > 0) Thread.Sleep((int)Math.Round(sleep));
            else Thread.SpinWait(SPIN_WAIT_COUNT);
        }
    }

    private static void SendWheel(int mouseData)
    {
        var inp = new NativeMethods.INPUT
        {
            type = 0,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_WHEEL, mouseData = mouseData }
            }
        };
        NativeMethods.SendInput(1, [inp], Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendHWheel(int mouseData)
    {
        var inp = new NativeMethods.INPUT
        {
            type = 0,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_HWHEEL, mouseData = mouseData }
            }
        };
        NativeMethods.SendInput(1, [inp], Marshal.SizeOf<NativeMethods.INPUT>());
    }

    public void Dispose() => Stop();

    public static double ComputeEasingFraction(double dtMs, double duration, EasingMode mode, double tailToHeadRatio, bool easingEnabled)
    {
        if (!easingEnabled || mode == EasingMode.Linear)
        {
            return Math.Min(1.0, dtMs / duration);
        }

        var t = dtMs / duration;

        return mode switch
        {
            EasingMode.CubicOut => 1.0 - Math.Pow(1.0 - Math.Min(t, 1.0), 3),
            EasingMode.QuinticOut => 1.0 - Math.Pow(1.0 - Math.Min(t, 1.0), 5),
            _ => 1.0 - Math.Exp(-(2.0 + tailToHeadRatio) * t) // ExponentialOut (default)
        };
    }

    private struct Axis
    {
        public double RemainingPx;
        public long LastNotchTime;
        public int AccelFactor;
        public double UnitAccum;

        // Momentum fields
        public double Velocity;       // px/ms
        public bool InMomentum;
        private double _momentumAccum;

        public void RegisterNotch(long nowMs, int delta, AppSettings s)
        {
            // Cancel momentum on new user input
            if (InMomentum)
            {
                InMomentum = false;
                Velocity = 0;
                _momentumAccum = 0;
            }

            if (nowMs - LastNotchTime <= s.AccelerationDeltaMs)
                AccelFactor = Math.Min(s.AccelerationMax, Math.Max(1, AccelFactor + 1));
            else
                AccelFactor = 1;

            var timeSinceLast = nowMs - LastNotchTime;
            LastNotchTime = nowMs;

            var notches = delta / (double)WHEEL_DELTA;
            var pixels = notches * s.StepSizePx * AccelFactor;
            RemainingPx += pixels;

            // Track velocity for momentum
            if (s.MomentumEnabled && timeSinceLast > 0 && timeSinceLast < 500)
            {
                Velocity = pixels / timeSinceLast;
            }
        }

        public int Step(double dtMs, AppSettings s)
        {
            // Momentum phase: if normal scroll finished and velocity is significant
            if (s.MomentumEnabled && !InMomentum && Math.Abs(RemainingPx) < 0.1 && Math.Abs(Velocity) > 0.05)
            {
                var elapsed = Environment.TickCount64 - LastNotchTime;
                if (elapsed > 80) // Wait a short moment after last notch
                {
                    InMomentum = true;
                }
            }

            if (InMomentum)
            {
                // Friction: higher value = stops faster. Scale 0-100 to 0.001-0.02
                var friction = 0.001 + (s.MomentumFriction / 100.0) * 0.019;
                Velocity *= Math.Pow(1.0 - friction, dtMs);

                if (Math.Abs(Velocity) < 0.02)
                {
                    InMomentum = false;
                    Velocity = 0;
                    _momentumAccum = 0;
                    return 0;
                }

                var momentumPx = Velocity * dtMs;
                var wheelUnits = (momentumPx / BASE_STEP_PX) * WHEEL_DELTA;
                _momentumAccum += wheelUnits / EMIT_UNIT;

                int mPulses = 0;
                if (Math.Abs(_momentumAccum) >= 1.0)
                {
                    mPulses = (int)_momentumAccum;
                    _momentumAccum -= mPulses;
                }
                if (mPulses == 0) return 0;
                mPulses = Math.Clamp(mPulses, PULSE_CLAMP_MIN, PULSE_CLAMP_MAX);
                return mPulses * EMIT_UNIT;
            }

            // Normal smooth scroll
            if (Math.Abs(RemainingPx) < 0.1)
            {
                RemainingPx = 0;
                UnitAccum = 0;
                return 0;
            }

            var duration = Math.Max(1.0, s.AnimationTimeMs);
            var frac = ComputeEasingFraction(dtMs, duration, s.EasingMode, s.TailToHeadRatio, s.AnimationEasing);

            var emitPx = RemainingPx * frac;
            RemainingPx -= emitPx;

            var wUnits = (emitPx / BASE_STEP_PX) * WHEEL_DELTA;

            var units = wUnits / EMIT_UNIT;
            UnitAccum += units;

            int pulses = 0;
            if (Math.Abs(UnitAccum) >= 1.0)
            {
                pulses = (int)UnitAccum;
                UnitAccum -= pulses;
            }

            if (pulses == 0) return 0;
            pulses = Math.Clamp(pulses, PULSE_CLAMP_MIN, PULSE_CLAMP_MAX);
            return pulses * EMIT_UNIT;
        }
    }
}
