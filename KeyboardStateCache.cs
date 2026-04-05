using System;

namespace SoftScroll;

/// <summary>
/// Caches keyboard modifier key states to avoid repeated GetAsyncKeyState P/Invoke calls.
/// Updates at a fixed rate (60fps) when queried, rather than on every hook callback.
/// </summary>
internal sealed class KeyboardStateCache : IDisposable
{
    private const int UpdateIntervalMs = 16; // ~60fps

    private bool _shiftPressed;
    private bool _ctrlPressed;
    private bool _altPressed;
    private long _lastUpdateTick;

    /// <summary>
    /// Returns cached Shift key state. Updates if stale (>16ms since last check).
    /// </summary>
    public bool IsShiftPressed
    {
        get
        {
            UpdateIfStale();
            return _shiftPressed;
        }
    }

    /// <summary>
    /// Returns cached Ctrl key state. Updates if stale (>16ms since last check).
    /// </summary>
    public bool IsCtrlPressed
    {
        get
        {
            UpdateIfStale();
            return _ctrlPressed;
        }
    }

    /// <summary>
    /// Returns cached Alt key state. Updates if stale (>16ms since last check).
    /// </summary>
    public bool IsAltPressed
    {
        get
        {
            UpdateIfStale();
            return _altPressed;
        }
    }

    /// <summary>
    /// Force update all keys immediately. Call this at the start of processing
    /// if you need guaranteed fresh state for the current frame.
    /// </summary>
    public void ForceUpdate()
    {
        UpdateInternal();
    }

    private void UpdateIfStale()
    {
        var now = Environment.TickCount64;
        if (now - _lastUpdateTick >= UpdateIntervalMs)
        {
            UpdateInternal();
        }
    }

    private void UpdateInternal()
    {
        _shiftPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
        _ctrlPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        _altPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
        _lastUpdateTick = Environment.TickCount64;
    }

    public void Dispose()
    {
        // No resources to dispose - pure computation
    }
}
