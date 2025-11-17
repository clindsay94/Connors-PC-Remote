# Connor's PC Remote – Architecture & Delivery Plan

**Date:** 16 Nov 2025  
**Author:** GitHub Copilot (GPT-5.1-Codex Preview)  
**Scope:** Full-solution assessment across `CPCRemote.Core`, `CPCRemote.Service`, `CPCRemote.UI`, and `CPCRemote.Tests`, with packaging and deployment guidance for WinUI 3/.NET 10.

---

## 1. Domain-Driven Analysis & Ubiquitous Language

- **Bounded Context:** _Remote Power Management_ – Accept HTTP GET commands from Samsung SmartThings (or any HTTP client) to execute secure power actions (shutdown, restart, lock, turn screen off, UEFI reboot) on a Windows PC.
- **Aggregates:**
  - `TrayCommand` (Core) – encapsulates command metadata; aggregate root for command catalogue.
  - `RemoteSession` (implicit) – represented via `HostHelper`/`Worker`; governs authentication, authorization, and execution for a single request.
- **Domain Services:**
  - `CommandHelper`/`ITrayCommandHelper` – executes commands via shell/PInvoke.
  - `HostHelper` – validates inbound requests and orchestrates command execution.
- **Ubiquitous Terms:** _Command_, _Secret_, _Ping_, _Service Management_, _Service Installer_.
- **Security & Compliance:** Remote execution must enforce bearer-token authentication (Authorization header), log administrative actions, and require elevation for privileged operations. HTTPS is optional but recommended when exposing beyond localhost.

This plan keeps the ubiquitous language stable, ensures aggregates own their invariants, and protects boundaries (secret validation before any command invocation).

---

## 2. Current State Assessment (Per Layer)

| Layer                                         | Strengths                                                                                                                            | Gaps / Risks                                                                                                                                                                                                                                                                                                                                                          |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Domain/Core (`CPCRemote.Core`)**            | C# 14, nullable enabled, DI-first interfaces. `CommandHelper` already OS-gated.                                                      | Duplicate command catalog (`TrayCommandHelper` vs `CommandHelper`). Asynchronous orchestration handled via `Task.Run` in `HostHelper` rather than cooperative cancellation. Limited XML docs.                                                                                                                                                                         |
| **Application/Service (`CPCRemote.Service`)** | Worker service uses `BackgroundService`, `IOptionsMonitor`, exponential backoff, HTTPS certificate binding logic.                    | HttpListener-based pipeline lacks structured metrics, cancellation token propagation, and rate limiting. Configuration validator does not leverage data annotations. Netsh automation needs error classification and audit trail.                                                                                                                                     |
| **Presentation/UI (`CPCRemote.UI`)**          | WinUI 3 MSIX packaging, logging via `Microsoft.Extensions.Logging`, service management workflows, modern nav shell.                  | `ServiceManagementPage.xaml` corrupted backup still present; code-behind consolidates multiple responsibilities (service control, config editing, HTTP testing). No MVVM separation, no DI container. Quick Actions page instantiates `CommandHelper` directly (missing interface abstraction & centralized telemetry). Settings not persisted beyond local settings. |
| **Testing (`CPCRemote.Tests`)**               | NUnit + Moq, OS-gated tests, coverage for `HostHelper`/`TrayCommandHelper`.                                                          | No service integration tests, duplicated domain catalog tests, naming doesn’t follow `MethodName_Condition_ExpectedResult` consistently, no HTTPS/auth negative tests.                                                                                                                                                                                                |
| **Packaging/Delivery**                        | MSIX manifest already requests `runFullTrust`, service binaries included via `Link` item, .NET 10 global.json ensures SDK alignment. | Service not packaged separately for headless deployment, no CI pipeline scripts, certificate/signing workflow undocumented, no deterministic artifact layout (PublishSingleFile false, no zipped outputs).                                                                                                                                                            |

---

## 3. Gap Analysis vs Best Practices

1. **SOLID & Interface Segregation**

   - `ITrayCommandHelper` mixes metadata lookups with execution responsibilities; split into `ICommandCatalog` + `ICommandExecutor` to keep SRP and ease UI binding.
   - `HostHelper` should become an application service that consumes catalog + executor via DI, surfaces async APIs with cancellation.

2. **Async/Await & Resiliency**

   - Replace `Task.Run` invocations with true async command dispatch (e.g., `RunCommandAsync`).
   - Flow `CancellationToken` through Worker request pipeline; ensure `GetContextAsync` cancellation is honored to avoid hung tasks during shutdown.

3. **Configuration & Validation**

   - Model `RsmOptions` with data annotations (`[Required]`, `[Range]`, `[RegularExpression]`).
   - Use `OptionsBuilder.ValidateDataAnnotations()` plus custom `IValidateOptions` for cross-field rules (e.g., HTTPS requires certificate path/password pair).

4. **Security**

   - Document HTTPS binding process end to end, including certificate creation, `netsh` automation, and revocation.
   - Add structured logging for authorization failures, include remote endpoint info.
   - Consider optional request throttling (simple token bucket) to prevent abuse on exposed endpoints.

5. **WinUI Architecture**

   - Introduce lightweight MVVM (CommunityToolkit.Mvvm) for `ServiceManagementPage` and `QuickActionsPage` to separate UI from service orchestration, enabling unit testing.
   - Centralize HttpClient usage via `IHttpClientFactory` or dedicated `ServiceClient` to avoid stray sockets and improve telemetry.

6. **Testing & Tooling**

   - Add integration tests for `Worker` using `HttpClient` against an in-memory `HttpListener` substitute (or run on dynamic port) gated by platform attributes.
   - Expand NUnit naming to `MethodName_Condition_ExpectedResult`.
   - Wire tests into CI (GitHub Actions Windows agent) targeting net10.0-windows10.0.26100.0.

7. **Packaging & Deployment**
   - Produce three artifacts per release:
     1. **MSIX** for WinUI 3 shell (includes service payload under `ServiceBinaries/`).
     2. **Self-contained Windows Service bundle** (`dotnet publish /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true`).
     3. **Bootstrapper script** that installs/updates the service silently (PowerShell + `sc.exe` + `netsh`).
   - Document signing (`signtool.exe`) and App Installer integration for sideloading.

---

## 4. Implementation Roadmap (Phased)

### Phase 1 – Foundation Hardening (Core + Service)

- [x] **Task 1 · Command Catalog Consolidation**
  - Remove `TrayCommandHelper`; extend `CommandHelper` to expose immutable command metadata via `ICommandCatalog`.
  - Update tests and UI to consume catalog interface; ensure XML docs for public APIs.
- [x] **Task 2 · Async Command Execution**
  - Introduce `Task RunCommandAsync(TrayCommandType, CancellationToken)` with platform checks and error propagation.
  - Update `Worker` and `HostHelper` to await command execution and propagate cancellation.
- [ ] **Task 3 · Configuration Validation Enhancements**
  - Decorate `RsmOptions` with data annotations.
  - Inject `IValidateOptions<RsmOptions>` plus `.ValidateDataAnnotations()` to ensure consistent validation.
- [ ] **Task 4 · Security Logging & Telemetry**
  - Add structured logging scopes (`using var scope = _logger.BeginScope(new { Remote = context.Request.RemoteEndPoint })`).
  - Log unauthorized attempts at Warning level with throttling.

### Phase 2 – UI & UX Modernization (WinUI 3)

1. **Repair XAML + Introduce MVVM**
   - Remove corrupted `.xaml.corrupted` file; refactor `ServiceManagementPage` into View + ViewModel (DI via `AppHostBuilder`).
2. **Service Management Enhancements**
   - Provide progress feedback and cancellation for long-running operations (install/uninstall/reserve URL) using `Task` + `CancellationTokenSource`.
   - Persist configuration presets (IP, port, HTTPS, secrets) via `ApplicationData` or JSON file co-located with UI.
3. **Quick Actions Telemetry & Safety**
   - Inject `ICommandExecutor` via DI; centralize confirmation preferences.
   - Introduce optional `PIN`/`secret` before executing local commands to prevent accidental triggers.

### Phase 3 – Packaging, Deployment & Observability

1. **Artifact Pipeline**
   - Add `build.ps1` that runs `dotnet restore`, `dotnet test`, service publish, and MSIX packaging.
   - Generate `Artifacts/Service/win-x64/CPCRemote.Service.exe` (self-contained, single-file) and `Artifacts/UI/CPCRemote.UI.msixbundle`.
2. **Signing & Distribution**
   - Document certificate requirements (Dev cert vs production) and `signtool sign /fd SHA256 /a /tr http://timestamp.digicert.com` workflow.
3. **Installer Guidance**
   - Provide PowerShell script (`Install-CPCRemote.ps1`) that installs the service, reserves URL, configures firewall, and launches the WinUI shell.
4. **Observability**
   - Integrate EventSource or ETW for service metrics; optional Application Insights instrumentation using connection string from config.

---

## 5. Packaging & Deployment Strategy (Detailed)

### 5.1 Build Outputs

| Artifact                    | Command                                                                                                                                                                                                                                                                    | Notes                                                                                                                   |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| **Service Bundle**          | `dotnet publish .\CPCRemote.Service\CPCRemote.Service.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false /p:GenerateFullPaths=true /p:PublishTrimmed=false` | Produces `publish\CPCRemote.Service.exe` ready for `sc.exe`. Keep `appsettings.json` beside executable.                 |
| **UI MSIX**                 | `msbuild .\CPCRemote.UI\CPCRemote.UI.csproj /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Always /p:UapAppxPackageBuildMode=SideLoad /p:PackageCertificateThumbprint=<THUMBPRINT>`                                             | Requires developer certificate installed in `LocalMachine\My`. Output `.msixbundle` signed and ready for App Installer. |
| **Combined Zip (Optional)** | Script to zip `Service publish` + `MSIX` + `Install.ps1`.                                                                                                                                                                                                                  | Ideal for GitHub Releases; include checksums (SHA256).                                                                  |

### 5.2 Service Installation Script (Next Step)

1. Copy published service folder to target machine (e.g., `C:\Program Files\CPCRemote.Service`).
2. Run elevated PowerShell:

   ```powershell
   # Reserve URL
   netsh http add urlacl url=http://+:5005/ user="BUILTIN\Users"

   # Install/update service
   sc.exe stop "CPCRemote.Service" 2>$null
   sc.exe delete "CPCRemote.Service" 2>$null
   sc.exe create "CPCRemote.Service" binPath="C:\Program Files\CPCRemote.Service\CPCRemote.Service.exe" start=auto
   sc.exe description "CPCRemote.Service" "Connor's PC Remote HTTP listener"
   sc.exe start "CPCRemote.Service"
   ```

3. For HTTPS, bind certificate:

   ```powershell
   $thumb = "<PFX Thumbprint Without Spaces>"
   netsh http add sslcert ipport=0.0.0.0:5006 certhash=$thumb appid="{4fbdab34-09c3-4c3c-9219-61bff33f5d80}" certstorename=MY
   ```

### 5.3 MSIX Distribution Workflow

1. **Sign** the MSIX bundle with trusted code-signing cert.
2. **Generate App Installer** file referencing the bundle for easy sideload updates.
3. **Document Installation**: instruct admins to enable sideloading, install MSIX, then open “Connor’s PC Remote” to manage the service.
4. **Update Process**: reinstall MSIX (auto updates service payload), UI prompts to reinstall Windows service binaries if versions mismatch.

---

## 6. Testing & Quality Gates

1. **Unit Tests**: Expand coverage for new catalog/executor interfaces, configuration validation failure cases, and HTTPS binding logic (mock `netsh`).
2. **Integration Tests**: Launch `Worker` on ephemeral port inside tests, issue HTTP requests with/without Authorization header, validate responses.
3. **UI Smoke Tests**: Use WinAppDriver or Playwright for Windows to automate button flows (service install/reserve URL dialogs mocked via interfaces).
4. **Performance/Load**: Simple script hitting `ping` endpoint 50 req/s to ensure listener remains responsive; monitor CPU/memory.
5. **Security Checks**: Lint for secrets, ensure TLS settings documented; optional `dotnet list package --vulnerable` in CI.

---

## 7. Immediate Next Actions

1. **Adopt this plan** as the living source of truth; store updates here after each milestone.
2. **Create work items** (GitHub issues or Azure Boards) per task in Phases 1–3.
3. **Stand up CI** (GitHub Actions Windows-latest) executing `dotnet restore`, `dotnet test`, service publish, MSIX build (using `windows-latest` with Visual Studio Build Tools + Windows App SDK).
4. **Establish release cadence** (semantic versioning). Tag releases, attach MSIX & service bundle artifacts.

With these steps, Connor's PC Remote will stay aligned with modern C#/.NET 10 and WinUI 3 patterns, maintain SOLID/DDD integrity, and ship in a repeatable, secure manner without drifting from its core objective: trustworthy remote power control via HTTP.

## Change Tracking

- _2025-11-16 – GitHub Copilot (GPT-5.1-Codex Preview):_ Added checkbox tracking for Phase 1 tasks to support implementation progress reporting per `.copilot-tracking` workflow.
- _2025-11-16 – GitHub Copilot (GPT-5.1-Codex Preview):_ Completed Phase 1 Task 1 by consolidating the command catalog into `CommandHelper`, adding `ICommandCatalog`/`ICommandExecutor`, and updating UI/service/tests.
- _2025-11-16 – GitHub Copilot (GPT-5.1-Codex Preview):_ Completed Phase 1 Task 2 by introducing async command execution, propagating cancellation through `HostHelper` and the service worker, and extending tests.

---

End of plan.
