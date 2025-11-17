# Gemini Project: Connor's PC Remote

## Project Overview

This is a .NET 10 application for remotely controlling PC power functions via HTTP commands. The solution is comprised of four projects:

*   **CPCRemote.Core**: A class library containing the core logic and models.
*   **CPCRemote.Service**: A Windows Service that hosts the HTTP listener.
*   **CPCRemote.UI**: A WinUI 3 application for managing and interacting with the service.
*   **CPCRemote.Tests**: A unit test project using NUnit and Moq.

The application is built using the .NET 10 SDK, targeting Windows.

## Building and Running

### Build

To build the entire solution, run the following command from the root directory:

```powershell
dotnet build
```

### Test

To run the unit tests, use the following command:

```powershell
dotnet test
```

## Development Conventions

*   The solution is structured into four distinct projects, separating core logic, service, UI, and tests.
*   The projects target the `net10.0-windows10.0.26100.0` framework.
*   Unit tests are written using NUnit and Moq.
*   The UI is built with WinUI 3.
*   The service is a .NET Worker Service.
