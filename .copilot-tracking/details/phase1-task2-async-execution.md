<!-- markdownlint-disable-file -->

# Phase 1 · Task 2 – Async Command Execution

**Related Plan Section:** `ARCHITECTURE_AND_DELIVERY_PLAN.md` §4 · Phase 1 (Foundation Hardening)

## Objective

Propagate true asynchronous command execution across the domain and service layers so long-running OS calls can honor cancellation tokens. Replace ad-hoc `Task.Run` usage with `RunCommandAsync` semantics that flow `CancellationToken` from the `Worker` listener down to the OS-invocation layer.

## Required Changes

- Extend `ICommandExecutor` with `Task RunCommandAsync(TrayCommandType, CancellationToken)` and `Task RunCommandByNameAsync(object, CancellationToken)`; keep synchronous shims for legacy consumers if necessary.
- Refactor `CommandHelper` to implement the async methods by using `Process.Start` overloads that support waiting asynchronously (e.g., `WaitForExitAsync`) and P/Invoke operations wrapped in `ValueTask` helpers. Ensure cooperative cancellation before launching shutdown commands when possible.
- Update `HostHelper` to expose async APIs that accept a `CancellationToken` and delegate exclusively to async execution paths without blocking threads.
- Modify `Worker` to pass its `stoppingToken` through request handling, awaiting async execution of commands and respecting cancellation when shutting down the service.
- Introduce tests in `CPCRemote.Tests` that verify async pathways (mocking `ICommandExecutor` to confirm `RunCommandAsync` is invoked) and cover cancellation short-circuit behavior.
- Document the new async contract in XML comments and ensure DI registrations remain valid.

## Success Criteria

- No `Task.Run` remains in `CommandHelper`, `HostHelper`, or `Worker` for command execution; all paths use the async API with cancellation.
- The service stops gracefully when `stoppingToken` is triggered, canceling any in-flight command execution.
- Tests cover the async contract, including when cancellation is requested before execution.
- Plan and `.copilot-tracking` change log updated to reflect Task 2 completion.

## Notes

- Keep the synchronous `RunCommand` methods as convenience wrappers that call into async implementations with `CancellationToken.None` until callers are updated.
- Consider using `CancellationToken.ThrowIfCancellationRequested` prior to invoking OS commands to avoid partial execution during shutdown.
