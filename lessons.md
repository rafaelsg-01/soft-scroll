# Lessons Learned

Persistent knowledge base for SoftScroll project. Read at the start of every session.

---

## 2026-07-11 — Issue #13: Horizontal scroll bugs

### Bug 1: WM_MOUSEHWHEEL delta sign convention differs from Shift+WM_MOUSEWHEEL

**Symptom**: Scrolling thumb wheel right moved content left, and vice versa.

**Root cause**: SoftScroll emits horizontal scrolling by `PostMessageW(WM_MOUSEWHEEL + MK_SHIFT)` instead of `WM_MOUSEHWHEEL`. The two messages use **opposite** sign conventions:

| Event | `+delta` | `-delta` |
|---|---|---|
| `WM_MOUSEHWHEEL` (native) | Scroll right ➡️ | Scroll left ⬅️ |
| `WM_MOUSEWHEEL + MK_SHIFT` (Shift+vertical sim) | Wheel up → left ⬅️ | Wheel down → right ➡️ |

When the engine forwarded a positive HWHEEL delta unchanged, the target window interpreted it as "Shift + wheel up" → leftward. Hence the inversion.

**Fix**: Invert delta when emitting: `(uint)(-hMouseData) << 16 | MK_SHIFT`. Located in `Core/SmoothScrollEngine.cs::SendWheel`.

**Why we didn't switch to `MOUSEEVENTF_HWHEEL` (Option A in bug report)**: That path needs the active foreground window and isn't guaranteed to work with legacy apps. The minimal inversion is lower-risk and works with the same target windows that already receive Shift+WM_MOUSEWHEEL.

### Bug 2: Horizontal scroll dies when `HorizontalSmoothness` is disabled

**Symptom**: After unchecking "Horizontal Smoothness", side-button horizontal scrolling stopped working in every app.

**Root cause**: `MouseHWheel` handler in `App.xaml.cs` unconditionally set `args.Handled = true`. Meanwhile the engine worker thread set `outH = 0` when `HorizontalSmoothness == false`. Result: original event swallowed, no replacement emitted.

**Fix**: In `App.xaml.cs::MouseHWheel` handler, return early when `_settings.HorizontalSmoothness == false` without setting `Handled = true`. The native `WM_MOUSEHWHEEL` then passes through to the target window via `CallNextHookEx`.

### Rule going forward

**Never swallow a native event unless you have a positive path to re-emit it.** This applies to all four hook handlers in `App.xaml.cs` (`MouseWheel`, `MouseHWheel`, `MouseZoomWheel`, `MiddleButtonDown`). If a future "disable" setting is added, the handler must early-return without setting `Handled = true`. A safe pattern:

```
if (relevantSetting == false) return; // <-- must come BEFORE args.Handled = true
// ... rest of interception ...
args.Handled = true;
```

### Architectural lesson: dual-path inversion in input translation

Whenever code translates one Windows input message into another (e.g. HWHEEL → Shift+MWHEEL), the sign convention of the target message MUST be re-derived from scratch. Document the inversion at the call site with a comment naming both messages and their conventions, otherwise the next maintainer will reintroduce the bug while "simplifying".

---

## 2026-07-11 — Centralize app version (single source of truth)

### Symptom

The version string `0.3.2` was hard-coded in three places that had to be edited in lockstep:

1. `SmoothScrollClone.csproj` — `<Version>0.3.2</Version>`
2. `Settings/SettingsWindow.xaml.cs` — `TxtVersion.Text = "Version 0.3.2"`
3. `installer/SoftScroll.iss` — `#define MyAppVersion "0.3.2"`

The GitHub Actions workflow `build.yml` partially mitigated this by replacing strings at release time, but it was fragile (regex on `[\d.]+`), drifted from `release-please` config, and required manual sync between local dev builds and CI builds.

### Fix

The .NET SDK already generates `AssemblyVersion`, `AssemblyFileVersion`, and `AssemblyInformationalVersion` from csproj `<Version>` properties. The fix is therefore to **never hard-code the version in source code** — always read it from assembly metadata at runtime.

- Added `Infrastructure/AppVersion.cs` with two properties:
  - `AppVersion.Informational` → full string (e.g. `0.3.2+abc123`).
  - `AppVersion.Short` → trimmed semver (`0.3.2`), suitable for UI display.
  - Reads `AssemblyInformationalVersionAttribute` first, falls back to `FileVersionInfo` (works for single-file published apps).
- `SettingsWindow.xaml.cs` now does `TxtVersion.Text = $"Version {AppVersion.Short};"`.
- csproj now declares all four version properties explicitly (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`, `<InformationalVersion>`) so the displayed version is unambiguous and the git SHA is auto-appended for diagnostic builds.
- `installer/SoftScroll.iss` is the **only** file that still needs a string literal, because Inno Setup's preprocessor runs at compile time before any runtime metadata is available. The release workflow now reads the version from csproj via regex instead of trusting a `steps.version` output, so the workflow no longer has to be edited when bumping versions.

### Rule going forward

**Anywhere a value is derived from project metadata (version, product name, company, copyright, repository URL, etc.), read it from assembly attributes or other auto-generated sources. Never duplicate it as a string literal in `.cs`/`.xaml` files.**

If you find yourself writing `"Version 0.3.2"` in code, add the lookup to `Infrastructure/AppVersion.cs` (or a sibling helper) instead.