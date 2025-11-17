<!-- markdownlint-disable-file -->

# Phase 1 · Task 1 – Command Catalog Consolidation

**Related Plan Section:** `ARCHITECTURE_AND_DELIVERY_PLAN.md` §4 · Phase 1 (Foundation Hardening)

## Objective

Eliminate the duplicate command catalog (`TrayCommandHelper`) and ensure `CommandHelper` is the single source of truth for command metadata and execution. Surface immutable command metadata through a dedicated `ICommandCatalog` abstraction so UI and service layers can depend on read-only data contracts.

## Required Changes

- Define `ICommandCatalog` in `CPCRemote.Core` exposing:
  - `IReadOnlyList<TrayCommand>` (immutable list of commands)
  - Lookup helpers for `TrayCommandType` ⇄ display text conversions.
- Update `CommandHelper` to implement `ICommandCatalog` and return immutable metadata.
- Remove `TrayCommandHelper` and migrate all references (Core, UI, and Tests) to the new abstraction.
- Ensure dependency injection registers `CommandHelper` behind the new interface (alongside execution interface if needed).
- Update NUnit tests and WinUI components to consume `ICommandCatalog` rather than concrete helpers.
- Add XML documentation for all newly public members and updated APIs per repository guidelines.

## Success Criteria

- There is exactly one catalog of commands, exposed through `ICommandCatalog`.
- UI/test layers no longer reference `TrayCommandHelper`.
- Metadata is read-only to consumers (no accidental mutation from callers).
- Architecture plan is updated to reflect task completion, and `.copilot-tracking/changes` records the modified files.

## Notes

- Keep future Phase 1 Task 2 (async execution) in mind but defer its implementation to its dedicated task.
- Preserve existing command set and ordering so downstream UI remains stable.
