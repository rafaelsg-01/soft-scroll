# Changelog

All notable changes to this project will be documented in this file.

## [0.3.0](https://github.com/quangtruong2003/soft-scroll/compare/v0.2.0...v0.3.0) (2026-07-11)


### Features

* Add advanced settings architecture and expand AppSettings ([95960e4](https://github.com/quangtruong2003/soft-scroll/commit/95960e4d1ca643fa6a48a88620253a2aedae7b5d))
* Add exclusion list improvements, Start with Windows, and UI enhancements (v0.2.0) ([e225bbb](https://github.com/quangtruong2003/soft-scroll/commit/e225bbb677acd769b179b75e148a1abf5683ae84))
* Add global hotkey toggle and per-app profiles support ([3eae60d](https://github.com/quangtruong2003/soft-scroll/commit/3eae60de0abbf1af2979d74e54f26552afe5cb5f))
* Add localization support, integrate Serilog for logging, and introduce new application settings. ([19de85a](https://github.com/quangtruong2003/soft-scroll/commit/19de85ab02c89efe209de6f7aac8c60a0252ec77))
* add release-please for auto releases with semantic versioning ([7174d79](https://github.com/quangtruong2003/soft-scroll/commit/7174d79c0c9e22aff30b2aade6ef47ac45a3399d))
* Add scroll indicator, accessibility announcements, and expand settings UI ([408c6cd](https://github.com/quangtruong2003/soft-scroll/commit/408c6cd5c638038dd6fc77d3e5d0951082c21477))
* centralize app version via assembly metadata ([9b6fbcc](https://github.com/quangtruong2003/soft-scroll/commit/9b6fbcc8967270bdae1879e16a842192ada9a9ba))
* Expand settings, add hotkey toggle, per-app profiles, indicator, and accessibility ([a86746b](https://github.com/quangtruong2003/soft-scroll/commit/a86746be5642e4a97e00cdb9d265bb50abcc6b37))
* Implement smooth zoom and middle-click scroll features, add localization support, and refine the settings UI. ([77db171](https://github.com/quangtruong2003/soft-scroll/commit/77db17149489f0e09225f051c088ea20eb56b6cf))
* v0.2.0 - Exclusion list improvements, Start with Windows, and UI enhancements ([76517b6](https://github.com/quangtruong2003/soft-scroll/commit/76517b6e440e01da5031a3685a1164302bef116d))


### Bug Fixes

* Add IsOwnWindow check to prevent actions in the application's own window ([7645efe](https://github.com/quangtruong2003/soft-scroll/commit/7645efe0fa75c1e666cab0e353351f18d05b7d87))
* Address code review findings - thread safety, null refs, and race conditions ([5cb88ea](https://github.com/quangtruong2003/soft-scroll/commit/5cb88ea43b9d28f4033db711e4380b62bf3df782))
* **ci:** drop manifest-file input from release-please ([6af2dc1](https://github.com/quangtruong2003/soft-scroll/commit/6af2dc1d9e1c5084d91a0c5b916fcb474b529470))
* **ci:** release-please — use simple release-type with XML extra-files ([0d1cd33](https://github.com/quangtruong2003/soft-scroll/commit/0d1cd33121345efe4642abf98c93863bbe6d233c))
* **ci:** release-please-action v4 needs release-type: simple at action level ([c1ec800](https://github.com/quangtruong2003/soft-scroll/commit/c1ec8008d19ae56d5711dc92a16a8a9d9d094e28))
* **ci:** remove packages key from release-please-config ([fcae8c6](https://github.com/quangtruong2003/soft-scroll/commit/fcae8c6a292f83280f9827da04ecc5c6b24c326a))
* nullable warning in process exclusion lock and add Inno Setup installer ([5c9f522](https://github.com/quangtruong2003/soft-scroll/commit/5c9f522ee5487f7cdfb6211776efe7f2d02ada7e))
* remove TxtStepLabel/TxtMsLabel name references from DataTemplate ([b890dd9](https://github.com/quangtruong2003/soft-scroll/commit/b890dd9be15453695b99c3ade3b7fb7ebec6336a))
* suppress settings window when starting minimized with Windows ([0a62fb8](https://github.com/quangtruong2003/soft-scroll/commit/0a62fb802c7f2f756fb0ce9c39b7da51375cf587))


### Performance Improvements

* Improve scroll engines with adaptive frame rate and buffered input ([4188f10](https://github.com/quangtruong2003/soft-scroll/commit/4188f10dec32376b255f8ba602b79ef647a423c4))

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
