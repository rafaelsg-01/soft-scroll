using System;
using SoftScroll.Native;

namespace SoftScroll.Hooks;

public sealed class MouseWheelEventArgs : EventArgs
{
    public int Delta { get; }
    public bool Handled { get; set; }
    public MouseWheelEventArgs(int delta) => Delta = delta;
}

public sealed class MousePositionEventArgs : EventArgs
{
    public int X { get; }
    public int Y { get; }
    public MousePositionEventArgs(int x, int y) { X = x; Y = y; }
}

// Low-level mouse hook with ability to mark wheel events handled
public sealed class GlobalMouseHook : IDisposable
{
    private IntPtr _hook = IntPtr.Zero;
    private NativeMethods.HookProc? _proc;

    private static readonly KeyboardStateCache _keyboardState = new();

    public bool IsInstalled => _hook != IntPtr.Zero;

    /// <summary>
    /// When true, holding Shift will convert vertical scroll to horizontal.
    /// </summary>
    public bool ShiftKeyHorizontal { get; set; } = true;

    public event EventHandler<MouseWheelEventArgs>? MouseWheel;
    public event EventHandler<MouseWheelEventArgs>? MouseHWheel;
    public event EventHandler<MouseWheelEventArgs>? MouseZoomWheel;
    public event EventHandler<MousePositionEventArgs>? MiddleButtonDown;
    public event EventHandler? MiddleButtonUp;
    public event EventHandler<MousePositionEventArgs>? MouseMoved;

    public void Install()
    {
        if (IsInstalled) return;
        _proc = HookCallback;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (!IsInstalled) return;
        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            if ((data.flags & (NativeMethods.LLMHF_INJECTED | NativeMethods.LLMHF_LOWER_IL_INJECTED)) != 0)
                return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);

            if (msg == NativeMethods.WM_MOUSEWHEEL)
            {
                int delta = (short)((data.mouseData >> 16) & 0xffff);
                var args = new MouseWheelEventArgs(delta);

                if (_keyboardState.IsCtrlPressed)
                {
                    MouseZoomWheel?.Invoke(this, args);
                }
                else if (ShiftKeyHorizontal && _keyboardState.IsShiftPressed)
                {
                    MouseHWheel?.Invoke(this, args);
                }
                else
                {
                    MouseWheel?.Invoke(this, args);
                }

                if (args.Handled)
                    return (IntPtr)1;
            }
            else if (msg == NativeMethods.WM_MOUSEHWHEEL)
            {
                int delta = (short)((data.mouseData >> 16) & 0xffff);
                var args = new MouseWheelEventArgs(delta);
                MouseHWheel?.Invoke(this, args);
                if (args.Handled)
                    return (IntPtr)1;
            }
            else if (msg == NativeMethods.WM_MBUTTONDOWN)
            {
                MiddleButtonDown?.Invoke(this, new MousePositionEventArgs(data.pt.x, data.pt.y));
            }
            else if (msg == NativeMethods.WM_MBUTTONUP)
            {
                MiddleButtonUp?.Invoke(this, EventArgs.Empty);
            }
            else if (msg == NativeMethods.WM_MOUSEMOVE)
            {
                MouseMoved?.Invoke(this, new MousePositionEventArgs(data.pt.x, data.pt.y));
            }
        }
        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
