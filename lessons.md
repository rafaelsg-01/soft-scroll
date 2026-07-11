# Lessons Learned

Persistent knowledge base for SoftScroll project. Read at the start of every session.

---

## 2026-07-11 — Issue #13: Shift+wheel regression introduced by horizontal scroll fix

### Symptom (regression reported by user on build from commit `be8fdc8`)

After applying the side-button horizontal-scroll direction fix (`75c4251a`), the previously-working Shift+vertical-wheel-as-horizontal stopped working: scrolling wheel up while holding Shift now scrolled right instead of left.

### Root cause analysis

The previous fix (`75c4251a`) added a single sign inversion at one point in the pipeline to make the side-button (real `WM_MOUSEHWHEEL`) direction match physical intent. But horizontal scrolling in this app has **two distinct entry points** that converge at the same `MouseHWheel` event and the same `SendWheel` emission:

1. **Real HWHEEL event** — `WM_MOUSEHWHEEL` from the side-button. Native convention: `+delta` = scroll right.
2. **Shift+vertical wheel conversion** — `WM_MOUSEWHEEL` reinterpreted as horizontal when Shift is held. Native convention: `+delta` (wheel up) = scroll left (because target windows convert Shift+wheel-up into horizontal-left).

These two paths were treated identically inside `OnHWheel` → `SendWheel`, but the **target window's interpretation of the emitted message depends on which path produced it**:

| Path | Native source delta | Meaning at source | After inversion in `SendWheel` | Meaning at target |
|---|---|---|---|---|
| Side-button | `+120` (right) | right | `-120` (Shift+wheel down) | right ✅ |
| Shift+vertical wheel up | `+120` (up) | up → intended left | `-120` (Shift+wheel down) | right ❌ |

A single inversion in `SendWheel` makes only one path correct and silently breaks the other. The architecture mistake was treating these two sign conventions as identical when they are not.

### The fix (two changes that must land together)

1. **Keep** the `(-hMouseData)` inversion in `Core/SmoothScrollEngine.cs::SendWheel`. This corrects the side-button path.
2. **Add** a matching inversion in `Hooks/GlobalMouseHook.cs::HookCallback` when forwarding a Shift+vertical wheel event to `MouseHWheel`. This compensates for the `SendWheel` inversion, restoring the original Shift+wheel direction.

Both inversions cancel out for Shift+wheel events; only the side-button path ends up with a single net inversion. Verified by tracing all 6 event combinations (vertical wheel, Shift+vertical wheel, side-button × smoothness on/off).

### Architectural rule going forward

**Never apply a single sign inversion on a path that has multiple upstream sources with different sign conventions.** If a hook callback can route to the same handler from two different Windows messages, identify them by message type at the boundary and apply per-source normalization there, not once at the shared emission point.

Apply the same rule to all four hook handlers in `App.xaml.cs` (`MouseWheel`, `MouseHWheel`, `MouseZoomWheel`, `MiddleButtonDown`). Each must early-return without setting `Handled = true` when its corresponding feature is disabled — never swallow a native event unless you have a positive path to re-emit it.

### Verification checklist before declaring any horizontal-scroll fix complete

1. Trace **side-button** direction (HWHEEL) — should match user's physical tilt.
2. Trace **Shift+vertical wheel** direction — should match Shift+MWHEEL convention at target (wheel up = left).
3. Trace **vertical wheel** without modifiers — must be unaffected.
4. Trace **Ctrl+wheel** (zoom) — must be unaffected.
5. Verify `HorizontalSmoothness = false` passes the native `WM_MOUSEHWHEEL` through unchanged.
6. Run `dotnet build` — must report 0 warnings, 0 errors.

### Engineering lesson: dual-path input translation

Whenever code translates one Windows input message into another (e.g. HWHEEL → Shift+MWHEEL, vertical wheel → Shift+MWHEEL), the sign convention of the **target** message must be re-derived from scratch at the translation site, not propagated through any shared normalization. Document both messages and their conventions in a comment at the call site so future maintainers don't reintroduce the bug while "simplifying".

### Files changed

- `Hooks/GlobalMouseHook.cs` — invert `delta` when forwarding Shift+vertical wheel to `MouseHWheel`.
- `App.xaml.cs` — early-return in `MouseHWheel` handler when `!HorizontalSmoothness`, leaving `Handled = false` so native event passes through.
- `Core/SmoothScrollEngine.cs` — invert `hMouseData` in `SendWheel` when computing `wParam` for `PostMessageW`.