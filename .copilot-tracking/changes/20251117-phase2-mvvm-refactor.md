<!-- markdownlint-disable-file -->

# Release Changes: Phase 1 & 2 – Foundation Hardening & UI Modernization

**Related Plan**: ARCHITECTURE_AND_DELIVERY_PLAN.md
**Implementation Date**: 2025-11-17

## Summary

Completed Phase 1 Task 1 by consolidating the command catalog into `CommandHelper`, introducing dedicated catalog/executor interfaces, updating DI/consumers, and removing the legacy helper implementation. Completed Task 2 by propagating asynchronous command execution across `HostHelper`, the service worker, and accompanying tests so cancellation flows from the Windows service through the OS command runners. Completed Task 3 by enhancing configuration validation using data annotations and `IValidateOptions`. Completed Task 4 by adding structured logging and throttling for unauthorized requests.

Completed Phase 2, Task 1 by refactoring the `ServiceManagementPage` to use the MVVM pattern, improving separation of concerns and testability.
Completed Phase 2, Task 1.5 by refactoring the `QuickActionsPage` to use the MVVM pattern, introducing `QuickActionsViewModel` and `RelayCommand` for power actions.

## Changes

### Added

- `.copilot-tracking/details/phase1-task1-command-catalog.md` - Documented the detailed requirements and success criteria for the catalog consolidation task.
- `.copilot-tracking/details/phase1-task3-config-validation.md` - Documented the detailed requirements and success criteria for the configuration validation task.
- `.copilot-tracking/details/phase1-task4-security-logging.md` - Documented the detailed requirements and success criteria for the security logging task.
- `.copilot-tracking/details/phase2-task1-mvvm-refactor.md` - Documented the detailed requirements and success criteria for the MVVM refactoring task.
- `CPCRemote.Core/Interfaces/ICommandCatalog.cs` - Added read-only command catalog abstraction for metadata discovery.
- `CPCRemote.Core/Interfaces/ICommandExecutor.cs` - Added execution abstraction to decouple command invocation from metadata consumers.
- `CPCRemote.Service/Options/CertificatePathRequiredAttribute.cs` - Added custom validation attribute for certificate path.
- `CPCRemote.UI/ViewModels/ServiceManagementViewModel.cs` - Added a view model for the `ServiceManagementPage`.
- `CPCRemote.UI/ViewModels/QuickActionsViewModel.cs` - Added a view model for the `QuickActionsPage`.

### Modified

- `.copilot-tracking/changes/20251116-phase1-foundation-changes.md` - Logged progress and summarized files impacted by Task 1.
- `ARCHITECTURE_AND_DELIVERY_PLAN.md` - Marked Phase 1 Task 1 complete and recorded the change in the tracking section.
- `CPCRemote.Core/Helpers/CommandHelper.cs` - Centralized catalog data, implemented new interfaces, and hardened execution logic.
- `CPCRemote.Core/Models/TrayCommand.cs` - Converted to an immutable record with documentation to back the catalog contract.
- `CPCRemote.Core/Helpers/HostHelper.cs` - Consumed `ICommandCatalog`/`ICommandExecutor` through DI-friendly abstractions.
- `CPCRemote.Service/Program.cs` - Registered `CommandHelper` behind the new interfaces for service consumption.
- `CPCRemote.Service/Worker.cs` - Switched request handling to rely on catalog lookups and executor dispatch.
- `CPCRemote.UI/QuickActionsPage.xaml.cs` - Removed code-behind logic and connected the view to the view model.
- `CPCRemote.UI/QuickActionsPage.xaml` - Refactored to use data binding with the new view model.
- `CPCRemote.Tests/HostHelperTests.cs` - Adapted mocks/assertions to the new abstractions.
- `CPCRemote.Tests/TrayCommandTests.cs` - Validated catalog behaviors via the new interfaces and immutable command data.
- `.copilot-tracking/changes/20251116-phase1-foundation-changes.md` - Documented completion of Task 2 and the associated async execution updates.
- `ARCHITECTURE_AND_DELIVERY_PLAN.md` - Marked Task 2 complete and added the async execution milestone to the change tracking section.
- `CPCRemote.Core/Helpers/HostHelper.cs` - Rebuilt the helper to parse secrets safely and await `ICommandExecutor.RunCommandAsync` with cooperative cancellation.
- `CPCRemote.Service/Worker.cs` - Reimplemented the HTTP listener to await async command execution, handle cancellation tokens, and keep the retry logic intact.
- `CPCRemote.Tests/HostHelperTests.cs` - Updated unit tests to verify `RunCommandAsync` invocation and secret validation behavior.
- `CPCRemote.Service/Options/RsmOptions.cs` - Decorated with data annotations for validation.
- `CPCRemote.Service/Options/RsmOptionsValidator.cs` - Simplified validator.
- `CPCRemote.Service/Program.cs` - Updated to use `.ValidateDataAnnotations()`.
- `CPCRemote.Service/CPCRemote.Service.csproj` - Added `Microsoft.Extensions.Options.DataAnnotations` package.
- `CPCRemote.Service/Worker.cs` - Added structured logging and throttling for unauthorized requests.
- `CPCRemote.UI/ServiceManagementPage.xaml` - Refactored to use data binding with the new view model.
- `CPCRemote.UI/ServiceManagementPage.xaml.cs` - Cleaned up code-behind.
- `CPCRemote.UI/App.xaml.cs` - Registered view models for DI.
- `CPCRemote.UI/CPCRemote.UI.csproj` - Added `CommunityToolkit.Mvvm` and `Microsoft.Extensions.DependencyInjection` packages.
- `CPCRemote.UI/Services/SettingsService.cs` - Updated to support both packaged (MSIX) and unpackaged (Debug) execution modes.

### Modified

- `.copilot-tracking/changes/20251116-phase1-foundation-changes.md` - Logged progress and summarized files impacted by Task 1.
- `ARCHITECTURE_AND_DELIVERY_PLAN.md` - Marked Phase 1 Task 1 complete and recorded the change in the tracking section.
- `CPCRemote.Core/Helpers/CommandHelper.cs` - Centralized catalog data, implemented new interfaces, and hardened execution logic.
- `CPCRemote.Core/Models/TrayCommand.cs` - Converted to an immutable record with documentation to back the catalog contract.
- `CPCRemote.Core/Helpers/HostHelper.cs` - Consumed `ICommandCatalog`/`ICommandExecutor` through DI-friendly abstractions.
- `CPCRemote.Service/Program.cs` - Registered `CommandHelper` behind the new interfaces for service consumption.
- `CPCRemote.Service/Worker.cs` - Switched request handling to rely on catalog lookups and executor dispatch.
- `CPCRemote.UI/Pages/QuickActionsPage.xaml.cs` - Updated quick actions page to use the catalog/executor abstractions for confirmations and execution.
- `CPCRemote.Tests/HostHelperTests.cs` - Adapted mocks/assertions to the new abstractions.
- `CPCRemote.Tests/TrayCommandTests.cs` - Validated catalog behaviors via the new interfaces and immutable command data.
- `.copilot-tracking/changes/20251116-phase1-foundation-changes.md` - Documented completion of Task 2 and the associated async execution updates.
- `ARCHITECTURE_AND_DELIVERY_PLAN.md` - Marked Task 2 complete and added the async execution milestone to the change tracking section.
- `CPCRemote.Core/Helpers/HostHelper.cs` - Rebuilt the helper to parse secrets safely and await `ICommandExecutor.RunCommandAsync` with cooperative cancellation.
- `CPCRemote.Service/Worker.cs` - Reimplemented the HTTP listener to await async command execution, handle cancellation tokens, and keep the retry logic intact.
- `CPCRemote.Tests/HostHelperTests.cs` - Updated unit tests to verify `RunCommandAsync` invocation and secret validation behavior.
- `CPCRemote.Service/Options/RsmOptions.cs` - Decorated with data annotations for validation.
- `CPCRemote.Service/Options/RsmOptionsValidator.cs` - Simplified to remove redundant checks.
- `CPCRemote.Service/Program.cs` - Updated to use `.ValidateDataAnnotations()`.
- `CPCRemote.Service/CPCRemote.Service.csproj` - Added `Microsoft.Extensions.Options.DataAnnotations` package.
- `CPCRemote.Service/Worker.cs` - Added structured logging and throttling for unauthorized requests.
- `CPCRemote.UI/Pages/ServiceManagementPage.xaml` - Refactored to use data binding with the new view model.
- `CPCRemote.UI/Pages/ServiceManagementPage.xaml.cs` - Removed code-behind logic and connected the view to the view model.
- `CPCRemote.UI/App.xaml.cs` - Registered the `ServiceManagementViewModel` for dependency injection.
- `CPCRemote.UI/CPCRemote.UI.csproj` - Added `CommunityToolkit.Mvvm` and `Microsoft.Extensions.DependencyInjection` packages.
- `CPCRemote.UI/QuickActionsPage.xaml` - Refactored to use data binding with the new view model.
- `CPCRemote.UI/QuickActionsPage.xaml.cs` - Removed code-behind logic and connected the view to the view model.
- `CPCRemote.UI/App.xaml.cs` - Registered the `QuickActionsViewModel` for dependency injection.

### Removed

- `CPCRemote.Core/Helpers/TrayCommandHelper.cs` - Deleted redundant catalog helper now that `CommandHelper` is the single source of truth.
- `CPCRemote.Core/Interfaces/ITrayCommandHelper.cs` - Replaced by separate catalog and executor abstractions.

## Release Summary

**Total Files Affected**: 26

### Files Created (8)

- `.copilot-tracking/details/phase1-task1-command-catalog.md` - Task-specific implementation brief.
- `.copilot-tracking/details/phase1-task3-config-validation.md` - Task-specific implementation brief.
- `.copilot-tracking/details/phase1-task4-security-logging.md` - Task-specific implementation brief.
- `.copilot-tracking/details/phase2-task1-mvvm-refactor.md` - Task-specific implementation brief.
- `CPCRemote.Core/Interfaces/ICommandCatalog.cs` - Catalog interface definition.
- `CPCRemote.Core/Interfaces/ICommandExecutor.cs` - Command execution interface definition.
- `CPCRemote.Service/Options/CertificatePathRequiredAttribute.cs` - Custom validation attribute.
- `CPCRemote.UI/ViewModels/ServiceManagementViewModel.cs` - View model for the `ServiceManagementPage`.

### Files Modified (16)

- `.copilot-tracking/changes/20251116-phase1-foundation-changes.md` - Updated progress log.
- `ARCHITECTURE_AND_DELIVERY_PLAN.md` - Reflected Task 1, 2, 3 and 4 completion and documented changes.
- `CPCRemote.Core/Helpers/CommandHelper.cs` - Implemented unified catalog + executor logic.
- `CPCRemote.Core/Models/TrayCommand.cs` - Introduced immutable record semantics.
- `CPCRemote.Core/Helpers/HostHelper.cs` - Updated dependencies to new abstractions.
- `CPCRemote.Service/Program.cs` - Wired DI registrations for new interfaces.
- `CPCRemote.Service/Worker.cs` - Consumed catalog/executor for HTTP command execution.
- `CPCRemote.UI/Pages/QuickActionsPage.xaml.cs` - Updated quick actions to leverage catalog/executor.
- `CPCRemote.Tests/HostHelperTests.cs` - Adjusted tests to mock new interfaces.
- `CPCRemote.Tests/TrayCommandTests.cs` - Verified catalog lookups through new abstractions.
- `CPCRemote.Service/Options/RsmOptions.cs` - Decorated with data annotations.
- `CPCRemote.Service/Options/RsmOptionsValidator.cs` - Simplified validator.
- `CPCRemote.Service/CPCRemote.Service.csproj` - Added new package reference.
- `CPCRemote.UI/Pages/ServiceManagementPage.xaml` - Refactored to use data binding.
- `CPCRemote.UI/Pages/ServiceManagementPage.xaml.cs` - Cleaned up code-behind.
- `CPCRemote.UI/App.xaml.cs` - Registered view model for DI.
- `CPCRemote.UI/CPCRemote.UI.csproj` - Added new package references.

### Files Removed (2)

- `CPCRemote.Core/Helpers/TrayCommandHelper.cs` - Removed duplicate command catalog implementation.
- `CPCRemote.Core/Interfaces/ITrayCommandHelper.cs` - Removed obsolete combined interface in favor of catalog/executor split.

### Dependencies & Infrastructure

- **New Dependencies**:
  - `Microsoft.Extensions.Options.DataAnnotations`
  - `CommunityToolkit.Mvvm`
  - `Microsoft.Extensions.DependencyInjection`
- **Updated Dependencies**: None
- **Infrastructure Changes**: None
- **Configuration Updates**: None

### Deployment Notes

Implementation in progress; details will be populated as tasks close.
