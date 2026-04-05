using System.Threading;
using SoftScroll.Native;

namespace SoftScroll.Hooks;

/// <summary>
/// Background-thread keyboard state sampler.
/// Polls modifier keys at fixed rate so the hook callback (hot path)
/// can read them without any P/Invoke overhead.
/// </summary>
public sealed class KeyboardStateSampler
{
    private const int PollIntervalMs = 16; // ~60fps

    private Thread? _thread;
    private volatile bool _running;
    private volatile bool _shift;
    private volatile bool _ctrl;
    private volatile bool _alt;

    public bool IsShiftPressed => _shift;
    public bool IsCtrlPressed => _ctrl;
    public bool IsAltPressed => _alt;

    public void ForceUpdate()
    {
        _shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
        _ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        _alt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        ForceUpdate(); // get initial state immediately
        _thread = new Thread(WorkerLoop) { IsBackground = true, Name = "KeyboardStateSampler" };
        _thread.Start();
    }

    private void WorkerLoop()
    {
        while (_running)
        {
            _shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
            _ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
            _alt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
            Thread.Sleep(PollIntervalMs);
        }
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(500);
    }
}
