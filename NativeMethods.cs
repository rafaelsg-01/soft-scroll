using System;
using System.Runtime.InteropServices;

namespace SoftScroll;

/// <summary>
/// Centralized Win32 P/Invoke signatures and struct definitions.
/// Prevents duplication across engines, hooks, and helpers.
/// </summary>
internal static class NativeMethods
{
    // ── Mouse Hook ─────────────────────────────────────────────────

    internal const int WH_MOUSE_LL = 14;
    internal const int WM_MOUSEWHEEL = 0x020A;
    internal const int WM_MOUSEHWHEEL = 0x020E;
    internal const int WM_MBUTTONDOWN = 0x0207;
    internal const int WM_MBUTTONUP = 0x0208;
    internal const int WM_MOUSEMOVE = 0x0200;

    internal const int LLMHF_INJECTED = 0x00000001;
    internal const int LLMHF_LOWER_IL_INJECTED = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public nint dwExtraInfo;
    }

    internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    // ── Input Injection ────────────────────────────────────────────

    internal const int INPUT_MOUSE = 0;
    internal const int INPUT_KEYBOARD = 1;

    internal const int MOUSEEVENTF_WHEEL = 0x0800;
    internal const int MOUSEEVENTF_HWHEEL = 0x01000;
    internal const int KEYEVENTF_KEYUP = 0x0002;

    internal const ushort VK_CONTROL = 0x11;
    internal const ushort VK_SHIFT = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT { public int type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] internal MOUSEINPUT mi;
        [FieldOffset(0)] internal KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public int time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public int dwFlags;
        public int time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll")]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int vKey);

    // ── Window / Process Queries ───────────────────────────────────

    internal const uint GA_ROOT = 2;

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    internal static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
}
