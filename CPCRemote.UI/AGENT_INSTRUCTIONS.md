# CPCRemote.UI – Agent Guide

## Mission

Provide a WinUI 3 management shell for installing/configuring the service and issuing local quick actions, packaged as an MSIX.

## Architecture Expectations

1. **MVVM Adoption** – Move code-behind logic into view models (CommunityToolkit.Mvvm or equivalent). Views should focus on layout/binding.
2. **Service Abstraction** – Interact with the Windows service through injectable facades (e.g., `IServiceController`, `IHttpClientFactory`). Avoid direct static calls inside views.
3. **Bootstrap Discipline** – Keep Windows App SDK bootstrapper logic centralized (`BootstrapHelper`). Don’t duplicate initialization steps in pages.
4. **State Persistence** – Store user preferences (theme, confirmations, endpoints) using `ApplicationData` or dedicated config files. Do not add new JSON files without documenting their location.
5. **Accessibility & UX** – Maintain keyboard navigation, provide status feedback (InfoBars/ProgressRing), and gate destructive actions behind confirmations.

## Implementation Checklist

- Update `ServiceManagementPage` to repair corrupted XAML and wire commands via view models.
- Route HTTP test calls through a shared client with proper timeout/retry settings.
- Ensure service install/uninstall buttons request elevation when needed and report errors clearly to users.
- When referencing Core abstractions (command catalog/executor), resolve them via DI to keep the UI testable.
- Keep assets (icons, styles) organized; prefer theme resources rather than hard-coded colors.

## Packaging Guidance

- Maintain MSIX configuration in `CPCRemote.UI.csproj` and `Package.appxmanifest`. Any capability changes must be justified and documented.
- Include updated service binaries under `ServiceBinaries/` during packaging (see plan for artifact pipeline).
- Sign MSIX outputs during release builds. Record certificate thumbprints + instructions in docs when they change.

## Testing

- Add UI smoke tests (Playwright for Windows/WinAppDriver) where practical; otherwise, document manual validation steps.
- Ensure Quick Actions that trigger local power events prompt the user; simulate actions via mocks when writing tests.

Treat this project as the face of the product—changes should improve clarity, reliability, or deployment ease.
