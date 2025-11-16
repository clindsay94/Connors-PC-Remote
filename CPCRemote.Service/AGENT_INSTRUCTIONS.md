# CPCRemote.Service – Agent Guide

## Mission

Host the Windows service that authenticates HTTP requests and executes commands through the Core abstractions.

## Key Principles

1. **Minimal Surface Area** – Expose only necessary endpoints (`/ping`, command verbs). Avoid adding ad-hoc routes without updating the architecture plan.
2. **Security-First** – Always require Authorization headers when `secret` is configured. Document any deviation and update tests.
3. **Robust Configuration** – Enforce validation via `IValidateOptions<RsmOptions>` plus data annotations. Reject invalid config during startup (fail fast).
4. **Async & Cancellation** – Propagate `CancellationToken` through `HttpListener`, certificate binding, and command execution to guarantee graceful shutdowns.
5. **Observability** – Use structured logging with scopes (remote endpoint, command, correlation ID). Keep secrets out of logs.

## Implementation Steps

1. Read `ARCHITECTURE_AND_DELIVERY_PLAN.md` Phase 1 tasks before coding.
2. When modifying request handling:
   - Validate headers/query/path early.
   - Short-circuit unauthorized requests with 401 + `WWW-Authenticate` header.
   - Map command text to domain types via Core catalog; never rely on `Enum.Parse` without validation.
3. Certificate / HTTPS changes must include:
   - Updates to `RsmOptions` + validator.
   - Documentation in `README.md` + packaging plan.
   - Script/runbook changes if `netsh` usage differs.
4. Any new background activity (e.g., metrics publishing) belongs in a dedicated `BackgroundService` registered in `Program.cs`.

## Testing

- Extend `CPCRemote.Tests` with integration-style tests that simulate `HostHelper` behavior or mock `HttpListener` inputs.
- Add regression tests whenever altering authorization, retry logic, or netsh automation.
- Manual validation: `dotnet publish` + run the worker locally with sample `appsettings.Development.json` when touching hosting code.

## Deployment/Packaging

- Keep service publish profile aligned with the plan (self-contained `win-x64`).
- Document required `sc.exe`/`netsh` commands whenever service installation behavior changes.
- If you modify logging targets or Application Insights integration, describe the instrumentation key handling in docs.

This project is the security boundary for remote control—treat every change with production rigor.
