# CPCRemote.Core – Agent Guide

## Purpose

Owns the **domain model** for Connor’s PC Remote. All command definitions, domain services, and shared abstractions originate here.

## Guardrails

1. **DDD First** – Treat `TrayCommand` and related value objects as aggregates. Any new business rule must live here before leaking into Service/UI projects.
2. **Interface Segregation** – Split responsibilities:
   - `ICommandCatalog` (metadata discovery)
   - `ICommandExecutor` (power action execution)
   - `ICommandSecurity` (future-proofing auth helpers)
     Update consumers incrementally, but never push UI-specific APIs down into Core.
3. **Async Enablement** – Add `Task RunCommandAsync(..., CancellationToken)` implementations. Synchronous wrappers may exist but must delegate to async logic.
4. **Windows APIs** – Keep `[SupportedOSPlatform]` attributes accurate. Isolate P/Invoke logic in partial classes so unit tests can mock the public surface.

## Implementation Checklist

- Update `TrayCommandType` + catalog entries **together**, and extend tests under `CPCRemote.Tests` immediately.
- Centralize command metadata (names, descriptions, safety flags) in immutable arrays/records. Provide lookup helpers with culture-insensitive comparisons.
- Add XML documentation for all public types/members (per repo instructions).
- Guard parameters with `ArgumentNullException.ThrowIfNull`/`string.IsNullOrWhiteSpace` as appropriate.
- Keep methods <50 LOC where possible; extract helpers for P/Invoke and process invocation.

## Testing

- Use NUnit `[Test]`/`[TestCase]` patterns mirroring `MethodName_Condition_ExpectedResult` naming.
- Mock `ICommandExecutor`/`ICommandCatalog` rather than relying on real shutdown/restart actions.
- Gate Windows-specific tests with `[SupportedOSPlatform("windows10.0.22621.0")]` to satisfy analyzers.

## Documentation

- When introducing new domain capabilities, update `README.md` (command list) and `ARCHITECTURE_AND_DELIVERY_PLAN.md` (Phase 1 items) as needed.

Stay focused on producing clean, reusable abstractions—everything else in the solution depends on this layer.
