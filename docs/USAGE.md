# Soft Scroll - Usage

## First run
- Run `SoftScroll.exe`
- The Settings window opens automatically
- Adjust parameters and click Save

## Tray icon
- Left-click: open Settings
- Right-click: menu (Enable, Exit)

## Settings explained
- Step size [px]: total pixels per notch (base amount before acceleration)
- Animation time [ms]: how long the smooth glide lasts per gesture
- Acceleration delta [ms]: time window to increase factor when spinning wheel quickly
- Acceleration max [x]: maximum acceleration factor
- Tail to head ratio [x]: shapes how much of the motion remains in the tail (higher = longer tail)
- Animation easing: use smooth ease-out
- Horizontal smoothness: also animate horizontal wheel
- Shift key horizontal scrolling: hold Shift to scroll horizontally (planned)
- Reverse wheel direction: invert wheel direction

## Tips
- If you see double scroll, make sure "Enable" is on and restart the app
- Some games may conflict with injected input; exit the app while gaming
- You can tweak Animation time and Tail/Head for your preference
