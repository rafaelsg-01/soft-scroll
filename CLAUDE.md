# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Soft Scroll** is a Windows desktop utility (.NET 8 WPF) that provides macOS-like smooth scrolling. It installs a `WH_MOUSE_LL` global low-level mouse hook, swallows native wheel events, and re-emits them as smoothed animation pulses via `SendInput`.

No tests exist in this project. No linter/formatter is configured.

## Commands

```bash
# Build (Debug)
dotnet build

# Run
dotnet run

# Publish single-file portable .exe to ./dist/SoftScroll.exe
dotnet publish -p:PublishProfile=Properties/PublishProfiles/SoftScrollSingleFile.pubxml
```

The pre-build `KillRunningInstance` target in the `.csproj` kills any running `SoftScroll.exe` to avoid file locks during build.

Settings are persisted to `%AppData%/SoftScroll/settings.json`. Logs are written to `%AppData%/SoftScroll/logs/` (Serilog, 7-day retention).

## Architecture

### Event Pipeline

```
GlobalMouseHook (WH_MOUSE_LL)
  ├── MouseWheel     ──→ SmoothScrollEngine    ──→ SendInput (wheel pulses at 120fps)
  ├── MouseHWheel    ──→ SmoothScrollEngine.OnHWheel
  ├── MouseZoomWheel ──→ ZoomSmoothEngine       ──→ SendInput (Ctrl+wheel)
  ├── MiddleButtonDown/Up/Moved ──→ MiddleClickScrollEngine ──→ SendInput
  └── (swallows events when EventArgs.Handled = true)
```

### Core Components

| Component | Responsibility |
|-----------|---------------|
| `App.xaml.cs` | Composition root. Wires hook → engines → tray/UI. Manages lifecycle. |
| `GlobalMouseHook.cs` | Win32 `WH_MOUSE_LL` hook. Dispatches wheel/horizontal/zoom/middle-click events. Filters injected events via `LLMHF_INJECTED` flags. |
| `SmoothScrollEngine.cs` | 120fps background thread. Accumulates pixel targets per notch, applies acceleration + easing, emits fractional wheel pulses. Supports vertical & horizontal axes. Includes optional momentum (beta). |
| `ZoomSmoothEngine.cs` | 120fps background thread. Smooths Ctrl+wheel zoom with fixed CubicOut 150ms easing. |
| `MiddleClickScrollEngine.cs` | 120fps background thread. Converts mouse displacement (dead-zone compensated) into smooth scroll deltas via quadratic speed curve. |
| `AppSettings.cs` | POCO + persistence. JSON serialize/deserialize, preset factories (`CreatePreset`), exclusion matching. |
| `SettingsViewModel.cs` | MVVM ViewModel (`INotifyPropertyChanged`). Mirrors all `AppSettings` properties, fires `SettingsChanged` on any update. `Snapshot()` produces a fresh `AppSettings`. |
| `SettingsWindow.xaml(.cs)` | WPF settings UI. Two-way binding to `SettingsViewModel`. |
| `TrayIcon.cs` | Windows Forms `NotifyIcon`. Context menu: Settings, Enable/Disable, Exit. |
| `MiddleClickOverlay.xaml(.cs)` | Transparent WPF window. Visualizes scroll direction/speed during middle-click scroll. |

### Threading Model

- **Main UI thread**: WPF dispatcher (settings window, tray icon, overlay).
- **3 background threads**: One per engine, running at 120fps (`FRAME_MS = 1000/120`). Each uses `lock` for shared state and `volatile bool _running` for shutdown signaling. Workers use `ManualResetEventSlim` to sleep when idle, waking only on new wheel events. When work is pending, they loop at ~120fps with `Thread.Sleep`/`Thread.SpinWait`.

### P/Invoke

All Win32 interop is centralized in `NativeMethods.cs`. Previously duplicated across engines/hook/helper files.

### Utility Classes

| Class | Purpose |
|-------|---------|
| `ProcessHelper` | Gets process name under cursor via Win32 calls (used for exclusion list). |
| `CachedProcessHelper` | Caches process-under-cursor with 100ms TTL to avoid repeated Win32 calls on every wheel event. |
| `NativeMethods` | Centralized P/Invoke signatures and Win32 struct definitions. All interop lives here. |
| `StartupManager` | Adds/removes app from `HKCU\...\Run` registry key for auto-start. |
| `ThemeHelper` | Detects Windows light/dark mode via registry; provides hardcoded color palettes. |
| `LocalizationManager` | Manages language selection; loads `.resx` resource files. |
| `LoggingConfig` | Serilog setup (daily rolling logs, 7-day retention). |
| `Constants.cs` | `ScrollConstants` — centralized constants (`WHEEL_DELTA=120`, `EMIT_UNIT=12`, `FRAME_RATE=120`, etc.). |
| `EasingCurveCanvas.cs` | WPF drawing visual for easing curve preview in settings UI. |

### Key Design Decisions

- **Event swallowing**: When `args.Handled = true`, the hook returns `(IntPtr)1` to prevent Windows from delivering the native event.
- **Fractional accumulator**: Engines accumulate fractional wheel units and only emit integer pulses when `|accum| >= 1.0`, reducing `SendInput` overhead without losing precision.
- **Injected event filtering**: `GlobalMouseHook` ignores events with `LLMHF_INJECTED` or `LLMHF_LOWER_IL_INJECTED` flags to prevent feedback loops from its own `SendInput` calls.
- **Per-process exclusion**: Checks `ProcessHelper.GetProcessUnderCursor()` on every wheel event against the `ExcludedApps` list (case-insensitive exact match).
- **Settings sync pattern**: `SettingsViewModel` mirrors `AppSettings` fields. Changes fire `SettingsChanged`, which in `App` takes a `Snapshot()` and pushes to all engines via `ApplySettings()`.
