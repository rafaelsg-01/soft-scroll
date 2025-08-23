# Soft Scroll

## Download

One-click download (Windows):

https://github.com/rafaelsg-01/soft-scroll/releases/latest/download/SoftScroll.exe

Note: Because the executable is not code-signed, Windows SmartScreen may warn on first run. Click “More info” → “Run anyway”.

---

Soft Scroll is a small Windows utility that adds smooth, configurable scrolling across the system. I built it because I personally missed macOS-like smoothness on Windows and decided to share it with everyone.

- Windows 10/11
- .NET 8 (WPF)
- Global low-level mouse hook + injected wheel deltas

## Quick start

1) Run `SoftScroll.exe` (no installer).
2) The app icon appears in the system tray.
3) Left-click the tray icon to open Settings.
4) Adjust parameters and click Save. Closing the window keeps the app running in the tray.

## Features

- Smooth wheel scrolling with adjustable parameters:
  - Step size [px]
  - Animation time [ms]
  - Acceleration delta [ms]
  - Acceleration max [x]
  - Tail to head ratio [x]
  - Animation easing (on/off)
  - Horizontal smoothness (on/off)
  - Shift key to force horizontal scrolling (planned)
  - Reverse wheel direction
- Tray app: left-click opens settings, right-click shows menu (Enable, Exit)
- Settings window opens automatically on first run

> This project is not affiliated with any commercial tool named “SmoothScroll”.

## How it works

Soft Scroll installs a low-level mouse hook (WH_MOUSE_LL). When a wheel event is detected, the original event is swallowed and a small background engine emits multiple smaller wheel pulses over time (via `SendInput`), applying an ease-out animation. Apps (browsers, explorer, editors) accumulate those deltas like regular scrolling, which feels much smoother.

## Build from source

Requirements: .NET 8 SDK, Windows 10/11.

- Clone the repository
- Open in Visual Studio 2022 (or run `dotnet build`)
- Run (F5)

Publish a single-file executable:

```
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

The exe will be at:

```
bin/Release/net8.0-windows/win-x64/publish/SoftScroll.exe
```

## Settings (suggested defaults)

- Step size [px]: 120
- Animation time [ms]: 360
- Acceleration delta [ms]: 70
- Acceleration max [x]: 7
- Tail to head ratio [x]: 3
- Animation easing: on
- Horizontal smoothness: on
- Reverse direction: off

Configuration file:

```
%AppData%/SoftScroll/settings.json
```

Use the “Reset All” button to restore defaults.

## Known limitations

- Some games or full-screen apps may not like injected wheel events. Consider exiting the app while gaming.
- Per-app include/exclude list is not implemented yet.
- Start with Windows is not implemented yet.

## Contributing

Contributions are welcome! Feel free to open issues, submit pull requests, or propose ideas. If you found this useful, a star helps others discover it.

## License

MIT
