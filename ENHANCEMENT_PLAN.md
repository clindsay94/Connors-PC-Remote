# Comprehensive CPC Remote Codebase Enhancement Prompt

## Role & Context

**Role:** CPC Bridge Architect  
**Tech Stack:** .NET 10 (C# 14), WinUI 3 (Windows App SDK 1.8), SmartThings Edge (Lua 5.3)  
**Workspaces:**

- `P:\Connor\Connors-PC-Remote` — C# Solution (Service, UI, Core, Tests)
- `C:\Users\Connor\CPCRemoteDriver` — SmartThings Edge Driver (Lua)

**Project Overview:**  
A distributed control system where a SmartThings Hub controls a Windows PC via HTTP. The Windows Service (`CPCRemote.Service`) listens for commands and reports hardware stats. A WinUI 3 desktop app (`CPCRemote.UI`) provides local management. The SmartThings Edge Driver sends HTTP requests to trigger power commands, launch apps, and poll hardware stats.

---

## Objectives

Perform a comprehensive codebase audit and enhancement covering **six workstreams**, all equally important:

1. **Code Quality Audit** — Fix all IDE warnings, unused code, and ensure .NET 10/C# 14 compliance
2. **Named Pipes IPC** — Replace HTTP communication between UI and Service with Named Pipes
3. **MSIX Packaging** — Configure self-signed certificate and ensure Release builds produce installable MSIX packages
4. **WinUI 3 UI Redesign** — Add animations, themes, live dashboard, and app catalog manager
5. **SmartThings Driver Audit** — Ensure Lua code is clean, consistent, and compliant
6. **Architecture Alignment** — Ensure both sides of the "bridge" stay in sync per the API contract

---

## Workstream Details

### 1. Code Quality Audit

**Scope:** All `.cs` files across `CPCRemote.Core`, `CPCRemote.Service`, `CPCRemote.UI`, `CPCRemote.Tests`

**Tasks:**

- Run `dotnet build` with `/warnaserror` and fix all warnings
- Remove all unused `using` statements
- Fix IDE analyzer warnings (IDE0028, IDE0044, IDE0060, CA1822, etc.)
- Ensure all public APIs have XML documentation comments
- Verify `.editorconfig` rules are being followed
- Use C# 14 features where beneficial:
  - Primary constructors
  - File-scoped namespaces
  - Collection expressions `[..]`
  - `System.Threading.Lock` instead of `object` for locks
  - Pattern matching and switch expressions
- Ensure `ConfigureAwait(false)` is used in library code (Core, Service) but NOT in UI code
- Verify nullable reference types are properly handled (use `is null` / `is not null`)

**Deliverables:**

- Zero warnings on `dotnet build`
- Clean code adhering to workspace `.editorconfig` and instruction files

---

### 2. Named Pipes IPC

**Scope:** Create inter-process communication between `CPCRemote.UI` and `CPCRemote.Service`

**Current State:**

- UI currently does NOT communicate with Service (planned feature)
- Service exposes HTTP endpoints for SmartThings only

**Tasks:**

- Create shared DTOs in `CPCRemote.Core` for IPC messages:
  - `IpcRequest` / `IpcResponse` base types
  - `Ge tStatsRequest` / `GetStatsResponse`
  - `GetAppsRequest` / `GetAppsResponse`
  - `SaveAppRequest` / `SaveAppResponse`
  - `ServiceStatusRequest` / `ServiceStatusResponse`
- Create `IPipeServer` interface and `NamedPipeServer` implementation in Service
- Create `IPipeClient` interface and `NamedPipeClient` implementation in UI
- Use `System.IO.Pipes.NamedPipeServerStream` / `NamedPipeClientStream`
- Pipe name: `CPCRemote_IPC`
- Implement JSON serialization for IPC messages
- Add DI registration for pipe services
- Handle connection lifecycle (connect, reconnect, timeout)

**Deliverables:**

- UI can query Service for real-time stats and app catalog
- UI can save/update app catalog entries via IPC
- UI can check Service status (running, listening address, etc.)

---

### 3. MSIX Packaging

**Scope:** `CPCRemote.UI` project and `Package.appxmanifest`

**Current State:**

- `Package.appxmanifest` exists with Publisher `CN=CLindsay94`
- `CPCRemote.UI_Key.pfx` exists (may need regeneration)
- `.csproj` has conditional `WindowsPackageType` for Debug vs Release

**Tasks:**

- Verify or regenerate self-signed certificate:

  ```powershell
  New-SelfSignedCertificate -Type Custom -Subject "CN=CLindsay94" -KeyUsage DigitalSignature -FriendlyName "CPCRemote Dev Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
  ```

- Export to `.pfx` and configure in `.csproj`:
  ```xml
  <PackageCertificateKeyFile>CPCRemote.UI_Key.pfx</PackageCertificateKeyFile>
  <PackageCertificateThumbprint>THUMBPRINT_HERE</PackageCertificateThumbprint>
  ```
- Verify `Package.appxmanifest` capabilities are correct
- Ensure Service binaries are included in MSIX package (already configured)
- Test Release build produces valid `.msix` file
- Document installation steps (trust certificate, install MSIX)

**Deliverables:**

- `dotnet publish -c Release` produces installable MSIX
- README updated with installation instructions

---

### 4. WinUI 3 UI Redesign

**Scope:** `CPCRemote.UI` project — Pages, Styles, ViewModels

**Current Pages:**

- `QuickActionsPage` — Power command buttons
- `ScheduledTasksPage` — Task scheduling (placeholder?)
- `ServiceManagementPage` — Service start/stop controls
- `SettingsPage` — Configuration

**Tasks:**

#### 4.1 New Dashboard Page

- Create `DashboardPage.xaml` as the default landing page
- Display live hardware stats (CPU %, Memory %, CPU Temp, GPU Temp)
- Use `RadialGauge` or `ProgressRing` controls for visual display
- Poll stats via Named Pipes IPC at configurable interval
- Show Service connection status (connected/disconnected indicator)
- Add quick action tiles for common commands

#### 4.2 App Catalog Manager

- Create `AppCatalogPage.xaml` for managing app launcher entries
- Display list of configured apps with edit/delete buttons
- Add/Edit dialog for app entries (Name, Path, Arguments, Category, RunAsAdmin)
- File picker for executable path
- Drag-and-drop reordering of slots
- Save changes via Named Pipes IPC

#### 4.3 Animations & Polish

- Add page transition animations (entrance/exit)
- Add button hover/press animations (scale, opacity)
- Use `AnimatedIcon` where available (Fluent icons)
- Consider Lottie animations for loading states
- Implement connected animations between pages where appropriate

#### 4.4 Theme Support

- Add Light/Dark/System theme toggle in Settings
- Create custom accent color picker
- Store theme preference in local settings
- Apply theme at app startup

#### 4.5 Navigation Updates

- Add Dashboard as first menu item
- Add App Catalog to navigation menu
- Update icons to use Segoe Fluent Icons consistently
- Consider collapsible navigation pane

**Deliverables:**

- Modern, animated UI with live dashboard
- Full app catalog management
- Theme customization
- Smooth page transitions

---

### 5. SmartThings Driver Audit

**Scope:** `C:\Users\Connor\CPCRemoteDriver\src\*.lua`

**Tasks:**

- Review `init.lua` for:
  - Consistent error handling
  - Proper nil checks before accessing state
  - Clean logging (remove debug statements if any)
- Review `comms.lua` for:
  - Timeout handling
  - Error response parsing
  - Content-Length header (already fixed)
- Review `dkjson.lua`:
  - Verify `_ENV` fix is stable
  - No other compatibility issues
- Check capability files match C# enum values:
  - `pcshutdown.yaml` commands match `TrayCommandType` enum
  - `appLauncher.yaml` slots match `AppCatalogEntry.Slot` values
- Verify profile (`cpc-remote-profile.yaml`) preferences are sensible
- Remove any dead code or unused capabilities

**Deliverables:**

- Clean, well-documented Lua code
- Capabilities in sync with C# API

---

### 6. Architecture Alignment

**Scope:** Cross-cutting concerns between C# and Lua

**API Contract (HTTP):**

| Endpoint          | Method | Description    | JSON Response                     |
| ----------------- | ------ | -------------- | --------------------------------- |
| `/ping`           | GET    | Health check   | (empty, 200 OK)                   |
| `/stats`          | GET    | Hardware stats | `{cpu, memory, cpuTemp, gpuTemp}` |
| `/apps`           | GET    | App catalog    | `[{slot, name, path, ...}]`       |
| `/launch/<Slot>`  | POST   | Launch app     | (empty, 200 OK)                   |
| `/<ShutdownMode>` | POST   | Power command  | (empty, 200 OK)                   |

**Tasks:**

- Verify all endpoints are documented in README
- Ensure Lua driver handles all response codes (200, 400, 401, 500)
- Add `/health` endpoint returning JSON with version, uptime, etc. (optional)
- Consider API versioning strategy for future (e.g., `/v1/stats`)

**Deliverables:**

- Clear API documentation
- Both sides handle edge cases gracefully

---

## Constraints & Guidelines

### C# Guidelines (from instruction files)

- Use C# 14 features and .NET 10
- PascalCase for public members, camelCase for private fields with `_` prefix
- File-scoped namespaces
- Primary constructors for DI
- `async`/`await` with `ConfigureAwait(false)` in non-UI code
- Guard clauses with `ArgumentNullException.ThrowIfNull()`
- Test naming: `MethodName_Condition_ExpectedResult()`

### Lua Guidelines (from instruction files)

- Always `require "dkjson"` for JSON
- Use `cosock` for HTTP requests
- Capability IDs must be camelCase and match filenames exactly
- Check `device.preferences.*` exists before use
- Never block the main thread

### Breaking Changes

- Breaking changes are ALLOWED
- This is a major enhancement — prioritize correctness over backward compatibility

---

## Success Criteria

1. **Zero build warnings** across entire solution
2. **Named Pipes IPC** working between UI and Service
3. **MSIX package** installs successfully on clean Windows machine
4. **Dashboard** displays live stats from Service
5. **App Catalog** can be managed from UI
6. **Animations** are smooth and enhance UX
7. **Theme switching** works correctly
8. **Lua driver** is clean and in sync with C# API

---

## Suggested Execution Order

1. **Code Quality Audit** — Clean foundation first
2. **Named Pipes IPC** — Enable UI-Service communication
3. **MSIX Packaging** — Ensure Release builds work
4. **UI Redesign** — Build on working IPC
5. **SmartThings Audit** — Verify bridge integrity
6. **Final Testing** — End-to-end validation

---

_Generated: November 24, 2025_
