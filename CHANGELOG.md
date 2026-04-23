# Changelog

All notable changes to this project will be documented in this file.

## [0.3.1] - 2026-03-17

### Added

- **Start Minimized** feature: Automatically hide window when starting with Windows (enabled by default)
- Serilog logging infrastructure for better debugging and diagnostics

### Changed

- Redesigned Settings UI with Windows 11 modern style:
  - Light theme color palette (#F3F3F3 background, #0078D4 accent)
  - Custom toggle switches instead of default checkboxes
  - Rounded corners (8px) on cards and buttons
  - Improved header with app icon and version display
  - Segmented control style for preset buttons
  - Better spacing and visual hierarchy
- Code quality improvements:
  - Fixed race condition in `SmoothScrollEngine.Stop()`
  - Replaced `Thread.Sleep(1)` with `SpinWait` for better CPU efficiency
  - Extracted magic numbers to `Constants.cs` class

### Fixed

- Race condition in `SmoothScrollEngine.Stop()` where axis state could be reset outside lock
- Input validation for excluded apps (trim whitespace, case-insensitive check)

## [0.3.0] - 2026-03-17

### Added

- Selectable easing curves: ExponentialOut (default), CubicOut, QuinticOut, Linear
- Quick Presets: Default, Reading, Productivity, Gaming — one-click scroll profiles
- Easing curve ComboBox in Settings UI

### Changed

- Modernized `App.xaml.cs` to file-scoped namespace and proper C# discards
- Improved thread safety in `SmoothScrollEngine` using `volatile` flag
- Modernized `SendInput` call to collection expression syntax
- Settings window height increased to accommodate new controls
- Version bump to 0.3.0

## [0.2.0] - 2026-01-19

### Added

- Add Application dialog showing running processes with icons
- "Browse..." button to select any executable file for exclusion list
- Per-app exclusion list (disable smooth scrolling for specific applications)
- "Start with Windows" feature (auto-launch via registry)
- Shift key to force horizontal scrolling
- Windows dark/light mode detection for adaptive UI theme
- Modern scrollbar styling that matches selected theme
- GitHub Actions CI workflow

### Changed

- Refactored namespace from `SmoothScrollClone` to `SoftScroll`
- Improved window detection using WindowFromPoint for accurate per-app exclusion
- Updated UI with cleaner color palette and hover effects
- Unified all UI text to English

### Fixed

- Exclusion list now correctly detects window under mouse cursor
- "Add" button for excluded apps properly tracks focused applications

### Removed

- Dead code (`MainWindow.xaml`, `MainWindow.xaml.cs`)

## [0.1.0] - 2025-08-22

- Initial public release
- Global wheel hook, smooth engine with easing and acceleration
- WPF tray app with settings UI and persistence
- Rebrand to "Soft Scroll"
