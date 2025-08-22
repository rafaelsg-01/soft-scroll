# Soft Scroll

Soft Scroll is a small Windows utility that adds smooth, configurable scrolling across the system. 
I built it because I personally missed macOS-like smoothness on Windows and decided to open it for anyone to use.

- Windows 10/11
- .NET 8 (WPF)
- Global low-level mouse hook + injected wheel deltas

## Features

- Smooth wheel scrolling with adjustable parameters:
  - Step size [px]
  - Animation time [ms]
  - Acceleration delta [ms]
  - Acceleration max [x]
  - Tail to head ratio [x]
  - Animation easing on/off
  - Horizontal smoothness on/off
  - Shift key to force horizontal scrolling
  - Reverse wheel direction
- Tray app: left-click opens settings, right-click shows menu (Enable, Exit)
- Starts with the settings window open (for first run convenience)

> Note: This project is not affiliated with any commercial "SmoothScroll" tool.

## How it works (high level)

Soft Scroll installs a low-level mouse hook (WH_MOUSE_LL). When a wheel event is detected, the original event is swallowed and a small background engine emits multiple smaller wheel pulses over time (via SendInput), creating an ease-out animation. The result is a much smoother feel, and apps (browsers, explorers, editors) accumulate the injected deltas just like normal scrolling.

## Download and run

1. Go to Releases (or build from source).
2. Download `SoftScroll.exe` (or the zipped publish folder) and run it.
3. You will see the app icon in the Windows tray.
4. Left-click the tray icon to open Settings.

No installer is required. To close the app, use the tray menu (Exit).

## Build from source

Requirements: .NET 8 SDK, Windows 10/11.

- Clone the repository
- Open the solution folder in Visual Studio 2022 (or run `dotnet build`)
- Run (F5)

To publish a single-file executable:

```
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

The exe will be in `bin/Release/net8.0-windows/win-x64/publish/SoftScroll.exe`.

## Settings

Open Settings (tray icon) and tweak parameters. Defaults are chosen for a balanced feel:

- Step size [px]: 120
- Animation time [ms]: 360
- Acceleration delta [ms]: 70
- Acceleration max [x]: 7
- Tail to head ratio [x]: 3
- Animation easing: on
- Horizontal smoothness: on
- Reverse direction: off

The configuration file is stored at:

```
%AppData%/SoftScroll/settings.json
```

Use the "Reset All" button to restore defaults.

## Known limitations

- Some games or full-screen apps may not like injected wheel events. Consider exiting while gaming.
- A per-application include/exclude list is not implemented yet.
- Starting with Windows is not implemented yet.

## Contributing / Como ajudar

Contribuições são bem-vindas! Se quiser ajudar, fique à vontade para abrir issues, enviar pull requests ou sugerir ideias. Se achou útil, deixe uma estrela para outras pessoas encontrarem o projeto.

## License

MIT
