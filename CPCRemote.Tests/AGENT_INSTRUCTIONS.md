# CPCRemote.Tests – Agent Guide

## Purpose

Guarantee regression coverage for core command logic, service orchestration, and UI-facing abstractions using NUnit + Moq.

## Testing Standards

1. **Naming** – Follow `MethodName_Condition_ExpectedResult` for every `[Test]` method. Use `[TestCase]` for parameterized scenarios.
2. **Structure** – Prefer AAA (Arrange/Act/Assert) without explicit comments; keep each test focused on one behavior.
3. **Platform Guards** – Decorate tests that rely on Windows-only APIs with `[SupportedOSPlatform("windows10.0.22621.0")]`.
4. **Mocking** – Mock interfaces from Core (`ICommandCatalog`, `ICommandExecutor`, `ITrayCommandHelper`). Avoid mocking framework classes like `HttpListener`; wrap them if necessary.
5. **Coverage** – Target critical flows:
   - Command catalog lookups & validation
   - Authorization logic (correct/incorrect secrets)
   - Configuration validation edge cases
   - Retry/exponential backoff behavior

## Workflow

1. Add/modify tests **before** implementing risky changes (TDD encouraged).
2. Keep helper methods private within the test class; share reusable builders via `TestUtilities` if duplication grows.
3. Use `Assert.Multiple` sparingly; prefer separate tests when feasible.
4. When tests rely on environment state (e.g., service installation), isolate with mocks/fakes instead of touching the OS.

## Tooling

- Run `dotnet test CPCRemote.Tests/CPCRemote.Tests.csproj --configuration Release` before pushing.
- Consider adding coverage tooling (`dotnet-coverage collect …`) when implementing large changes; store reports under `artifacts/coverage` (gitignored).

Reliable tests are the safety net for all other projects—treat failures as blockers and keep the suite fast.
