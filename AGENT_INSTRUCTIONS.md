# Connor's PC Remote – Agent Operating Guide

These instructions govern **all** agents contributing to this repository. Before modifying any project, read this file **and** the per-project guides (`*/AGENT_INSTRUCTIONS.md`) plus the master roadmap in `ARCHITECTURE_AND_DELIVERY_PLAN.md`.

## 1. Mission & Scope

- Deliver a trustworthy WinUI 3 + Windows Service solution that executes authenticated HTTP-triggered power actions.
- Maintain the intent documented in `README.md` and the architecture/delivery plan.
- Prefer incremental, reversible changes; each PR should tackle a single problem end to end (analysis → implementation → tests → docs).

## 2. Technical Baseline

- **Frameworks:** .NET 10, C# 14, Windows App SDK 1.8, WinUI 3 packaged as MSIX.
- **Platforms:** Windows 10 22621+ (service + UI). Assume x64 only.
- **Tooling:** `dotnet` CLI, MSBuild, MSTest/NUnit (tests project currently uses NUnit 4), Moq.
- **Security defaults:** Authorization header (Bearer). HTTPS strongly recommended when exposed beyond localhost.

## 3. Decision Guardrails

1. **DDD & SOLID** – Keep domain logic in `CPCRemote.Core`. Expose intent through interfaces; avoid leaking infrastructure concerns into the domain.
2. **Interface Segregation** – Do not extend `ITrayCommandHelper` with UI-only behaviors. Create purpose-built abstractions per project if needed.
3. **Async Everywhere** – Any new I/O (network, file, service control) must be async with cancellation tokens.
4. **Observability** – Log with `ILogger<T>`; include remote endpoint context for service requests and correlation IDs when adding cross-process flows.
5. **Configuration Safety** – Validate configuration on startup with `IValidateOptions` + data annotations. Never introduce silent fallbacks for secrets/certificates.
6. **Packaging Discipline** – Keep MSIX + service bundle build steps deterministic. Update the plan if packaging changes.

## 4. Standard Workflow

1. **Triage** – Link work to an item in the “Implementation Roadmap” (Phase 1–3). If no suitable entry exists, update `ARCHITECTURE_AND_DELIVERY_PLAN.md` first.
2. **Analysis** – Document root cause, affected layers, and acceptance criteria in the PR/issue. Reference section numbers from the plan when possible.
3. **Design** – For cross-cutting changes, draft a mini design note (Markdown) and store it under `docs/` or the relevant project directory.
4. **Implementation** – Follow per-project guidelines. Maintain XML documentation for public members. Keep methods under 50 LOC when practical.
5. **Testing** – Run `dotnet test` for impacted projects. Add/extend tests to cover new scenarios (especially auth, HTTPS, and command execution).
6. **Docs & Changelog** – Update `README.md`, project-specific guides, or `ARCHITECTURE_AND_DELIVERY_PLAN.md` to keep documentation truthful.
7. **Packaging Check** – If changes affect deployment, produce updated artifacts locally (service publish + MSIX) and attach logs to the PR.

## 5. Security Expectations

- Never log secrets, bearer tokens, or certificate passwords.
- Use Windows APIs responsibly; keep P/Invoke declarations `private` unless testing requires exposure.
- Ensure URL reservations (`netsh http add urlacl`) and SSL bindings are reflected in docs/scripts when modified.
- Treat every external command (e.g., `sc.exe`, `netsh`) as untrusted I/O; capture exit codes and error streams with context.

## 6. Testing & Quality Gates

- **Unit Tests:** Mandatory for new public APIs and bug fixes. Use NUnit 4 with `[Test]` / `[TestCase]` in `CPCRemote.Tests`.
- **Integration Tests:** When touching the HTTP pipeline or WinUI-service interaction, add/extend tests (even if gated by `[SupportedOSPlatform]`).
- **Static Analysis:** Respect analyzers enabled via `.editorconfig`. Fix warnings you introduce; do not suppress without justification.
- **Performance Checks:** For loops over command execution, measure before introducing caching/pooling.

## 7. Documentation Upkeep

- `ARCHITECTURE_AND_DELIVERY_PLAN.md` is the single source of truth for roadmap and packaging. Update it when strategy shifts.
- Each project directory hosts an `AGENT_INSTRUCTIONS.md` with tactical guidance—keep them synchronized with actual code.
- Record operational runbooks (e.g., installer scripts, certificate instructions) alongside the code they govern.

## 8. Communication

- Use descriptive commit messages: `<area>: <summary>` (e.g., `service: add replay protection`).
- Reference GitHub issues (or Google Jules tasks) directly in commits/PR descriptions.
- Surface open questions early; unresolved architecture decisions block implementation.

By following this guide plus the per-project instructions, agents ensure every contribution moves Connor’s PC Remote toward the “best version” envisioned in the roadmap.
