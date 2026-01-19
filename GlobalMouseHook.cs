using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SoftScroll;

public sealed class MouseWheelEventArgs : EventArgs
{
    public int Delta { get; }
    public bool Handled { get; set; }
    public MouseWheelEventArgs(int delta) => Delta = delta;
}

// Low-level mouse hook with ability to mark wheel events handled
public sealed class GlobalMouseHook : IDisposable
{
    private IntPtr _hook = IntPtr.Zero;
    private HookProc? _proc;

    public bool IsInstalled => _hook != IntPtr.Zero;

    /// <summary>
    /// When true, holding Shift will convert vertical scroll to horizontal.
    /// </summary>
    public bool ShiftKeyHorizontal { get; set; } = true;

    public event EventHandler<MouseWheelEventArgs>? MouseWheel;
    public event EventHandler<MouseWheelEventArgs>? MouseHWheel;

    public void Install()
    {
        if (IsInstalled) return;
        _proc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (!IsInstalled) return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            // Ignore injected events to avoid feedback loops
            const int LLMHF_INJECTED = 0x00000001;
            const int LLMHF_LOWER_IL_INJECTED = 0x00000002;
            if ((data.flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0)
                return CallNextHookEx(_hook, nCode, wParam, lParam);

            if (msg == WM_MOUSEWHEEL)
            {
                int delta = (short)((data.mouseData >> 16) & 0xffff);
                var args = new MouseWheelEventArgs(delta);

                // If Shift is held and ShiftKeyHorizontal is enabled, route to horizontal
                if (ShiftKeyHorizontal && IsShiftPressed())
                {
                    MouseHWheel?.Invoke(this, args);
                }
                else
                {
                    MouseWheel?.Invoke(this, args);
                }

                if (args.Handled)
                    return (IntPtr)1; // swallow
            }
            else if (msg == WM_MOUSEHWHEEL)
            {
                int delta = (short)((data.mouseData >> 16) & 0xffff);
                var args = new MouseWheelEventArgs(delta);
                MouseHWheel?.Invoke(this, args);
                if (args.Handled)
                    return (IntPtr)1; // swallow
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();

    private static bool IsShiftPressed()
    {
        const int VK_SHIFT = 0x10;
        return (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
