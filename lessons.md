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