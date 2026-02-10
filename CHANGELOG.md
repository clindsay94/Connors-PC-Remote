# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-02-10

### Added

#### Dashboard Color Customization

- New `CategoryColorService` for centralized sensor category color management
- Per-category color pickers in Settings (CPU, GPU, Memory, Motherboard, Storage, Cooling, Network, Other)
- Color choices persisted via `SettingsService` and applied globally to dashboard widgets and sensor cards
- "Reset All" button to restore default category colors

#### Gauge Improvements

- `CategoryBrush` dependency property on `AnimatedRadialGauge` — gauges now use their sensor's category color
- Glow ring correctly resizes with the gauge

### Changed

#### Dashboard Widget Layout

- `ShowRadialGauge`, `ShowLinearGauge`, and `ShowTallLayout` are now mutually exclusive — only one content mode renders at a time
- Tall mode uses a proper `2*:Auto:*` row split so gauge and value text occupy separate rows (cannot overlap)
- Increased `VariableSizedWrapGrid` cell size (220×200 → 240×220) with uniform 6px margins between cards
- Rebuilt `DashboardWidget.xaml` with proper visibility bindings and border clipping

#### ViewModel Updates

- Both `DashboardWidgetViewModel` and `SensorCardViewModel` now use `CategoryColorService` instead of hardcoded color switch expressions
- Static `SetColorService()` pattern for singleton initialization

### Fixed

- Widget overlap when resized to Wide or Tall configurations
- Text/gauge overlap in Tall mode widgets
- Content clipping in dashboard widgets

---

## [1.2.0] - 2026-02-09

### Added

#### Sensor Preferences

- Per-sensor visibility toggling (show/hide individual sensors on the dashboard)
- Drag-and-drop sensor reordering
- Sensor preferences persisted via IPC to the Service
- New `SensorPreferencesMessages` IPC protocol

#### Settings Overhaul

- Settings now support theme, accent color, backdrop material, font family, and font scale customization
- Typography settings with system font enumeration
- Dashboard-specific settings: refresh interval, temperature unit, animation toggle
- Window behavior settings: always on top, start minimized, remember position
- Firewall configuration UI with automatic rule management
- "Reset All Settings" with confirmation dialog

#### UI Infrastructure

- `SettingsService` for centralized settings persistence (packaged + unpackaged)
- `SecureStorageService` for credential protection with DPAPI
- Service configuration management via IPC (no more direct file editing)
- System tray icon with minimize-to-tray support

### Changed

#### Dashboard Rework

- Redesigned dashboard with sensor cards using category-colored gradients
- Animated radial gauges for percentage-based sensors
- Animated linear gauges for non-circular metrics
- Variable-sized widget grid (Square, Wide, Tall configurations)
- Real-time sensor value updates with configurable refresh interval

#### Architecture

- Full MVVM with CommunityToolkit.Mvvm partial properties
- Dependency Injection throughout all ViewModels and Pages
- Global exception handling with `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`

### Fixed

- Lock command failure from Session 0 (Windows Service context) — now uses `PInvoke` fallback
- Browse button in App Catalog now opens file picker correctly
- Font selection now applies globally across all pages
- Service Management page text overlap

---

## [1.1.0] - 2026-01-07

### Added

#### WiX Installer (CPCRemote.Installer)

- New WiX Toolset v6 installer project for traditional MSI-based deployment
- Automated Windows Service installation during MSI install
- Start Menu shortcut creation
- Per-machine installation to `Program Files\CPCRemote`
- Major upgrade support for seamless version updates
- GPO-compatible MSI for enterprise deployment

### Changed

#### Architecture Shift

- Migrated from MSIX packaging to WiX MSI installer
- CPCRemote.UI now builds as unpackaged application (`WindowsPackageType=None`)
- Self-contained deployment (~80 MB MSI) includes full .NET runtime

#### Build Process

- New three-step build process: Publish UI → Publish Service → Build Installer
- Added `CPCRemote.Installer.wixproj` to solution
- Simplified csproj with unconditional unpackaged configuration

### Technical Details

- WiX Toolset 6.0.2
- WixToolset.UI.wixext for minimal UI dialogs
- ICE03 validation suppressed for .NET runtime files with non-standard language metadata

## [1.0.1] - 2025-11-29

### Added

- Introduced `ConfigurationPaths` helper to standardize configuration and data file locations for both packaged and unpackaged deployments.
- Added comprehensive unit tests for `ConfigurationPaths` to ensure correct path resolution across all deployment scenarios.

### Changed

- Updated service and UI projects to use centralized configuration paths, ensuring all configuration files are stored in writable locations.

## [1.0.0] - 2025-11-29

### Added

#### Hardware Monitoring

- Real-time hardware stats endpoint (`GET /stats`) with configurable sensors
- HWiNFO shared memory integration for CPU temperature, GPU stats, RAM usage, and more
- Customizable sensor patterns via `appsettings.json` for matching system sensors
- JSON response with categorized hardware metrics

#### App Launcher System

- Remote app launching via HTTP (`GET /launch/{slot}`)
- App catalog management with 10 configurable slots
- `GET /apps` endpoint returns available applications as JSON
- UI page for configuring app catalog entries (name, path, arguments, admin privileges)
- Support for launching applications as administrator in user session (Session 0 isolation workaround)

#### Named Pipe IPC

- Inter-process communication between Service and UI via Named Pipes
- `NamedPipeServer` in Service for handling IPC requests
- `NamedPipeClient` in UI for seamless service communication
- Supports service status queries and app catalog synchronization

#### User Session Launcher

- `UserSessionLauncher` service for launching processes from Session 0 (Windows Service context)
- Token duplication and environment block handling for proper user session process creation
- Support for elevated (admin) and standard user process launches

#### Localization System

- `Resources.resx` with 75+ UI strings for future internationalization
- `LogMessages.resx` with 40+ structured logging messages
- Strongly-typed resource access via generated Designer classes
- All ViewModels updated to use localized strings

#### Configuration Validation

- `SensorOptionsValidator` implementing `IValidateOptions<SensorOptions>`
- Startup validation for sensor configuration patterns
- Clear error messages for misconfigured sensor matching rules

#### Service Constants

- `WorkerConstants` class with HTTP listener retry logic constants
- Configurable throttle delays and maximum retry attempts
- Better resilience for HTTP listener binding failures

#### Comprehensive Test Suite

- `AppCatalogServiceTests` — CRUD operations, JSON persistence, slot management
- `HardwareMonitorTests` — Response structure, configuration handling
- `NamedPipeClientTests` — Interface contracts, connection handling
- `NamedPipeServerTests` — IPC message serialization, request routing
- `SensorOptionsValidatorTests` — Pattern validation, edge cases
- `UserSessionLauncherTests` — Input validation, Session 0 behavior
- Total: 175+ unit tests with NUnit 4.4.0 and Moq 4.20.72

### Changed

#### UI Improvements

- Dynamic font enumeration using GDI+ interop (replaced Win2D dependency)
- Font family combo box in settings populated from system fonts
- Improved app catalog UI with better slot visualization

#### ViewModels Refactored

- `ServiceManagementViewModel` — HttpClient injection, 50+ strings localized
- `AppCatalogViewModel` — Localized status messages and error handling
- `DashboardViewModel` — Service status strings use Resources class
- `QuickActionsViewModel` — File-scoped namespace, XML documentation, localization

#### Code Quality

- Modern null-checking patterns (`is null`/`is not null`) throughout codebase
- File-scoped namespaces in all new and refactored files
- Enhanced XML documentation on public APIs
- `Directory.Build.props` improved with detailed comments explaining WarningsAsErrors configuration

#### Packaging

- UI project refactored for improved MSIX packaging
- Debug builds use `WindowsPackageType=None` for faster iteration
- Release builds produce proper MSIX packages

### Technical Details

#### Dependencies Updated

- .NET 10.0 (net10.0-windows10.0.26100.0)
- C# 14 language features
- Windows App SDK 1.8.x
- CommunityToolkit.Mvvm 8.4.0
- Microsoft.Extensions.Hosting.WindowsServices 10.0.0

#### Architecture

- Clean Architecture with Domain-Driven Design principles
- Options pattern for all configuration sections
- Dependency Injection throughout all components
- Background Service pattern for Windows Service host

## [0.1.0] - Initial Development

### Added

- Basic HTTP listener for power commands
- Core command execution (shutdown, restart, lock, screen off, force shutdown, UEFI reboot)
- WinUI 3 management interface
- Windows Service implementation
- Bearer token authentication
- HTTPS support with certificate binding
- Basic unit tests for core functionality

---

[1.3.0]: https://github.com/clindsay94/Connors-PC-Remote/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/clindsay94/Connors-PC-Remote/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/clindsay94/Connors-PC-Remote/compare/1.0.1...1.1.0
[1.0.1]: https://github.com/clindsay94/Connors-PC-Remote/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/clindsay94/Connors-PC-Remote/compare/main...New-Features
[0.1.0]: https://github.com/clindsay94/Connors-PC-Remote/releases/tag/v0.1.0