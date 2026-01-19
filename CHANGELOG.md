# Changelog

All notable changes to this project will be documented in this file.

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
