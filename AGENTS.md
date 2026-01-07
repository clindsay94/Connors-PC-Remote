# AGENTS.md

> This file provides AI coding agents with the context and instructions needed to work effectively on Connor's PC Remote.

## Project Overview

**Connor's PC Remote** is a Windows application for remotely controlling PC power functions via HTTP commands. It's built with **.NET 10** and **WinUI 3** (Windows App SDK 1.8).

### Key Technologies

- **Runtime**: .NET 10 (Windows 10/11, version 22621+)
- **UI Framework**: WinUI 3 with Windows App SDK 1.8
- **Architecture**: Clean Architecture with DDD principles
- **Testing**: NUnit 4.x with Moq
- **MVVM**: CommunityToolkit.Mvvm
- **Patterns**: Dependency Injection, Options Pattern, Background Services
- **Installer**: WiX Toolset v6 (MSI)

### Solution Structure

```
CPCRemote.sln
├── CPCRemote.Core/          # Shared library - commands, models, interfaces
│   ├── Enums/               # TrayCommandType and other enums
│   ├── Helpers/             # CommandHelper, HostHelper
│   ├── Interfaces/          # ICommandCatalog, ICommandExecutor
│   ├── IPC/                 # Inter-process communication
│   └── Models/              # AppCatalogEntry, TrayCommand, WolOptions
│
├── CPCRemote.Service/       # Windows Service - HTTP listener, IPC server
│   ├── Options/             # RsmOptions configuration
│   ├── Services/            # AppCatalogService, HardwareMonitor, NamedPipeServer
│   ├── Worker.cs            # BackgroundService with HttpListener
│   └── Program.cs           # Service host configuration
│
├── CPCRemote.UI/            # WinUI 3 desktop application (Unpackaged)
│   ├── Pages/               # XAML pages
│   ├── ViewModels/          # MVVM ViewModels (CommunityToolkit.Mvvm)
│   ├── Services/            # UI-specific services
│   ├── Helpers/             # UI utilities
│   └── Converters/          # XAML value converters
│
├── CPCRemote.Installer/     # WiX v6 MSI installer
│   ├── Product.wxs          # Package definition, components, features
│   └── CPCRemote.Installer.wixproj
│
└── CPCRemote.Tests/         # Unit tests (NUnit + Moq)
    ├── *Tests.cs            # Test files follow {ClassName}Tests.cs pattern
    └── Properties/
```

---

## Setup Commands

### Prerequisites

- **Windows 10/11** (version 22621 / 22H2 or later)
- **.NET 10 SDK** (`global.json` specifies version 10.0.100)
- **Visual Studio 2022 17.8+** with:
  - .NET desktop development workload
  - Windows App SDK (WinUI 3)
- **Administrator privileges** (for service installation/testing)

### Build Commands

```powershell
# Restore all dependencies
dotnet restore CPCRemote.sln

# Build entire solution (Debug)
dotnet build CPCRemote.sln

# Build entire solution (Release)
dotnet build CPCRemote.sln --configuration Release

# Build specific project
dotnet build CPCRemote.Service/CPCRemote.Service.csproj

# Clean and rebuild
dotnet clean CPCRemote.sln; dotnet build CPCRemote.sln
```

### Output Directories

Build outputs are unified under `bin/{Configuration}/{ProjectName}/`:

- `bin/Debug/CPCRemote.Core/`
- `bin/Debug/CPCRemote.Service/`
- `bin/Debug/CPCRemote.UI/`
- `bin/Debug/CPCRemote.Tests/`

---

## Development Workflow

### Running the Service (Debug)

```powershell
# Run the service directly (requires admin for HTTP listener binding)
dotnet run --project CPCRemote.Service/CPCRemote.Service.csproj
```

The service listens on the address configured in `appsettings.json` (default: `http://0.0.0.0:5005/`).

### Running the UI (Debug)

```powershell
# Run the WinUI 3 application
dotnet run --project CPCRemote.UI/CPCRemote.UI.csproj
```

Note: The UI project uses `WindowsPackageType=None` in Debug mode for easier development (no MSIX packaging).

### Configuration

Configuration is in `CPCRemote.Service/appsettings.json`:

```json
{
	"rsm": {
		"ipAddress": "0.0.0.0",
		"port": 5005,
		"secret": "",
		"useHttps": false,
		"certificateThumbprint": ""
	}
}
```

Key configuration sections:

- `rsm` - Remote Service Manager (HTTP listener settings)
- `wol` - Wake-on-LAN settings
- `monitor` - Hardware monitoring settings
- `sensors` - Sensor pattern matching configuration
- `apps` - Application catalog for remote launching

### URL Reservation (Windows)

If binding to non-localhost addresses, reserve the URL:

```powershell
# Run as Administrator
netsh http add urlacl url=http://+:5005/ user=EVERYONE
```

---

## Testing Instructions

### Test Framework

- **NUnit 4.x** - Primary test framework
- **Moq** - Mocking framework
- **Microsoft.NET.Test.Sdk** - Test host

### Running Tests

```powershell
# Run all tests
dotnet test CPCRemote.sln

# Run tests with verbose output
dotnet test CPCRemote.sln --logger "console;verbosity=detailed"

# Run specific test file
dotnet test CPCRemote.Tests/CPCRemote.Tests.csproj --filter "FullyQualifiedName~HostHelperTests"

# Run specific test by name
dotnet test CPCRemote.Tests/CPCRemote.Tests.csproj --filter "MethodName=TestMethodName"

# Run with coverage (if configured)
dotnet test CPCRemote.sln --collect:"XPlat Code Coverage"
```

### Test Naming Convention

Follow the pattern: `MethodName_Condition_ExpectedResult()`

```csharp
[Test]
public void GetCommandType_ValidCommand_ReturnsCorrectType()
{
    // Arrange
    var catalog = new CommandCatalog();

    // Act
    var result = catalog.GetCommandType("shutdown");

    // Assert
    Assert.That(result, Is.EqualTo(TrayCommandType.Shutdown));
}
```

### Test File Locations

- Test files are in `CPCRemote.Tests/`
- Named `{ClassName}Tests.cs` (e.g., `HostHelperTests.cs`, `RsmOptionsTests.cs`)
- Tests reference both `CPCRemote.Core` and `CPCRemote.Service` projects

### Existing Test Files

- `HostHelperTests.cs` - Tests for host/network helpers
- `PcStatsJsonTests.cs` - Tests for PC stats serialization
- `RsmOptionsTests.cs` - Tests for configuration options
- `ServiceConstantsTests.cs` - Tests for service constants
- `TrayCommandTests.cs` - Tests for command handling
- `WakeOnLanTests.cs` - Tests for WoL functionality

---

## Code Style Guidelines

### Core Conventions

See `.github/instructions/csharp.instructions.md` for detailed C# guidelines.

**Key points:**

- **C# Version**: Use C# 14 features (LangVersion 14 in Directory.Build.props)
- **Nullable Reference Types**: Enabled globally - use `is null` / `is not null`
- **File-scoped namespaces**: Preferred
- **Async/await**: Use for all I/O operations
- **Pattern matching**: Use switch expressions where appropriate

### Naming Conventions

| Element        | Convention                | Example             |
| -------------- | ------------------------- | ------------------- |
| Public members | PascalCase                | `ExecuteCommand()`  |
| Private fields | camelCase with underscore | `_logger`           |
| Interfaces     | I-prefix                  | `ICommandExecutor`  |
| Constants      | PascalCase                | `MaxRetryAttempts`  |
| Async methods  | Async suffix              | `RunCommandAsync()` |

### Architecture Guidelines

See `.github/instructions/dotnet-architecture-good-practices.instructions.md` for DDD and SOLID principles.

**Key architectural patterns in this project:**

1. **Dependency Injection**: Constructor injection throughout
2. **Options Pattern**: `IOptions<T>` / `IOptionsMonitor<T>` for configuration
3. **Background Services**: `BackgroundService` base class for `Worker.cs`
4. **MVVM**: ViewModels use `CommunityToolkit.Mvvm` with `[ObservableProperty]`

### Null-Safety Warnings as Errors

These null-related warnings are treated as errors (Directory.Build.props):

- `CS8602` - Dereference of possibly null reference
- `CS8603` - Possible null reference return
- `CS8604` - Possible null reference argument

### Code Analysis

- .NET analyzers enabled (`EnableNETAnalyzers=true`)
- Analysis level: `latest`
- Suppressed: `CA2007` (ConfigureAwait not required for Windows-only app)

---

## Build and Deployment

### Release Build

```powershell
# Build Release configuration
dotnet build CPCRemote.sln --configuration Release

# Publish self-contained Service
dotnet publish CPCRemote.Service/CPCRemote.Service.csproj -c Release -r win-x64 --self-contained

# Build MSIX package (UI project)
msbuild CPCRemote.UI/CPCRemote.UI.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxPackageDir="../publish/" /p:GenerateAppxPackageOnBuild=true
```

### Service Installation

```powershell
# Install as Windows Service (run as Administrator)
sc.exe create CPCRemote.Service binPath="C:\Path\To\CPCRemote.Service.exe" start=auto

# Start the service
sc.exe start CPCRemote.Service

# Stop the service
sc.exe stop CPCRemote.Service

# Remove the service
sc.exe delete CPCRemote.Service
```

---

## Pull Request Guidelines

### Title Format

```
[Component] Brief description
```

Examples:

- `[Service] Add HTTPS support with certificate binding`
- `[Core] Implement retry logic for command execution`
- `[UI] Fix service status refresh on settings page`
- `[Tests] Add unit tests for WakeOnLan helper`

### Pre-Commit Checklist

```powershell
# 1. Build succeeds
dotnet build CPCRemote.sln

# 2. All tests pass
dotnet test CPCRemote.sln

# 3. No new warnings (optional but recommended)
dotnet build CPCRemote.sln --warnaserror
```

### Commit Message Convention

Use conventional commits:

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

---

## Debugging and Troubleshooting

### Common Issues

#### Service Won't Start - Access Denied

```powershell
# Reserve URL for non-localhost binding
netsh http add urlacl url=http://+:5005/ user=EVERYONE
```

#### HTTP Listener Binding Fails

Check if another process is using the port:

```powershell
netstat -ano | findstr :5005
```

#### WinUI 3 XAML Errors

The UI project uses `DefineConstants=DISABLE_XAML_GENERATED_MAIN` for custom entry point in `Program.cs`.

#### Test Discovery Issues

Ensure `Microsoft.NET.Test.Sdk` package is present and `CopyLocalLockFileAssemblies=true` is set.

### Logging

- Service logs to Windows Event Log by default
- Log levels configured in `appsettings.json`
- Use `ILogger<T>` via DI for consistent logging

### Debugging the Service

1. Run from IDE with debugger attached
2. Or install as service and attach to `CPCRemote.Service.exe` process

---

## Project-Specific Context

### HTTP API Commands

Available endpoints (when authenticated):

- `GET /ping` - Health check
- `GET /shutdown` - Graceful shutdown
- `GET /restart` - Restart computer
- `GET /lock` - Lock workstation
- `GET /turnscreenoff` - Turn off display
- `GET /forceshutdown` - Force shutdown (10s delay)
- `GET /uefireboot` - Reboot to UEFI settings
- `GET /stats` - Get hardware stats (JSON)
- `GET /apps` - Get app catalog (JSON)
- `GET /launch/{slot}` - Launch configured app

### Authentication

Bearer token via `Authorization` header:

```
Authorization: Bearer your-secret-token
```

Or URL-based: `http://host:port/{secret}/{command}`

### IPC Communication

Named Pipe server (`NamedPipeServer.cs`) enables local communication between UI and Service.

### Windows-Specific APIs

Uses P/Invoke and Windows APIs for:

- Power management (`ExitWindowsEx`, `SetSuspendState`)
- Screen control (`SendMessage` with `WM_SYSCOMMAND`)
- Service control (`ServiceController`)
- Hardware monitoring (HWiNFO shared memory)

---

## Additional Notes

### Platform Target

All projects target **x64 only** (`<PlatformTarget>x64</PlatformTarget>`).

### Windows Version Requirements

- Minimum: Windows 10 22621 (22H2)
- Target: Windows 10 26100

### Package Dependencies to Note

- `Microsoft.WindowsAppSDK` 1.8.x - Core Windows App SDK
- `CommunityToolkit.Mvvm` 8.4.0 - MVVM helpers
- `CommunityToolkit.WinUI.*` 8.2.x - WinUI controls and helpers
- `Microsoft.Extensions.Hosting.WindowsServices` - Windows Service hosting

### Self-Contained Deployment

The Service project is configured for self-contained deployment (`SelfContained=true`), meaning .NET runtime is included in the output.

---

## Instruction Files Reference

Additional coding guidelines are in `.github/instructions/`:

| File                                                 | Purpose                            |
| ---------------------------------------------------- | ---------------------------------- |
| `csharp.instructions.md`                             | C# coding conventions and patterns |
| `dotnet-architecture-good-practices.instructions.md` | DDD and SOLID principles           |
| `implementation.instructions.md`                     | Task implementation tracking       |

These files are automatically applied by compatible AI coding tools.
