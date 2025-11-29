# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.0]: https://github.com/clindsay94/Connors-PC-Remote/compare/main...New-Features
[0.1.0]: https://github.com/clindsay94/Connors-PC-Remote/releases/tag/v0.1.0
