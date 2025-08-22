# Architecture

Soft Scroll consists of:

- WPF App (tray + settings UI)
- GlobalMouseHook (WH_MOUSE_LL): intercepts wheel events and exposes callbacks
- SmoothScrollEngine: background thread applying ease-out animation and injecting small wheel pulses using `SendInput`
- Persistence: JSON settings at `%AppData%/SoftScroll/settings.json`

Flow:
1. Wheel event arrives at hook
2. App swallows original event
3. Engine accumulates a target amount (pixels), applies acceleration, and animates emit over time
4. Engine injects many small wheel pulses to create smoothness

Notes:
- Horizontal smoothness is supported; Shift-to-horizontal can be implemented by rerouting vertical deltas when Shift is pressed
- Exclusion lists and startup-with-Windows can be added as future work
