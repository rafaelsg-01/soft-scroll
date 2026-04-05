using System;
using System.Runtime.InteropServices;

namespace SoftScroll;

public sealed class GlobalHotkey : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private IntPtr _hook = IntPtr.Zero;
    private NativeMethods.HookProc? _proc;
    private readonly int _id;
    private readonly uint _modifiers;
    private readonly ushort _key;
    private volatile bool _enabled = true;

    public event EventHandler? HotkeyPressed;

    public GlobalHotkey(int id, uint modifiers, ushort key)
    {
        _id = id;
        _modifiers = modifiers;
        _key = key;
    }

    public void Install()
    {
        if (_hook != IntPtr.Zero) return;

        _proc = HookCallback;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hook = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, _proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (_hook == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    public void SetEnabled(bool enabled) => _enabled = enabled;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _enabled)
        {
            var msg = wParam.ToInt32();
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                var vk = (ushort)((long)lParam >> 16 & 0xFFFF);

                bool ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                bool shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
                bool alt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;

                bool modCtrl = (_modifiers & 0x0002) != 0;
                bool modShift = (_modifiers & 0x0004) != 0;
                bool modAlt = (_modifiers & 0x0001) != 0;

                if (vk == _key &&
                    ctrl == modCtrl &&
                    shift == modShift &&
                    alt == modAlt)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    return (IntPtr)1;
                }
            }
        }
        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Uninstall();
    }
}

public static class HotkeyConstants
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const ushort VK_S = 0x53;
    public const ushort VK_F23 = 0xFC;
}
