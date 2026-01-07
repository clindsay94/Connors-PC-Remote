<!-- prettier-ignore -->
<div align="center">

<img src="./CPCRemote.UI/Assets/Square150x150Logo.png" alt="Connor's PC Remote" height="100" />

# Connor's PC Remote

_Remote PC power management via HTTP Request_

[![CI](https://github.com/clindsay94/Connors-PC-Remote/actions/workflows/ci.yml/badge.svg)](https://github.com/clindsay94/Connors-PC-Remote/actions)
![.NET 10](https://img.shields.io/badge/.NET-10-512bd4?style=flat-square&logo=dotnet)
![Windows](https://img.shields.io/badge/Windows-10%2F11-0078d4?style=flat-square&logo=windows)
![WinUI 3](https://img.shields.io/badge/WinUI-3-blue?style=flat-square)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)

[Features](#features) • [Requirements](#requirements) • [Getting Started](#getting-started) • [Usage](#usage) • [API Reference](#api-reference) • [Development](#development) • [Troubleshooting](#troubleshooting)

</div>

A Windows application for remotely controlling PC power functions via HTTP commands. Control shutdown, restart, lock, and more from any device on your network. Built with .NET 10 and WinUI 3.

## Features

- **Remote Power Control** — Shutdown, restart, lock, turn off screen, and UEFI reboot via HTTP
- **Windows Service** — Runs in background with automatic startup
- **Modern GUI** — WinUI 3 interface for easy configuration and testing
- **Secure Authentication** — Bearer token authentication with configurable secrets
- **App Launcher** — Launch configured applications remotely
- ~**HTTPS Support** — Optional TLS encryption with certificate binding~ >[!NOTE] >Coming Soon

## Architecture

```	
┌─────────────────┐     HTTP/HTTPS     ┌──────────────────────┐
│  Client Device  │ ◄─────────────────► │  CPCRemote.Service   │
│  (Phone, etc.)  │                     │  (Windows Service)   │
└─────────────────┘                     └──────────┬───────────┘
                                                   │
                                        Named Pipe │ IPC
                                                   │
                                        ┌──────────▼───────────┐
                                        │    CPCRemote.UI      │
                                        │   (WinUI 3 App)      │
                                        └──────────────────────┘
```

| Component                 | Description                                          |
| ------------------------- | ---------------------------------------------------- |
| **CPCRemote.Core**        | Shared library with commands, models, and interfaces |
| **CPCRemote.Service**     | Windows Service with HTTP listener and IPC server    |
| **CPCRemote.UI**          | WinUI 3 management application                       |
| **CPCRemote.Installer**   | WiX v6 MSI installer for deployment                  |
| **CPCRemote.Tests**       | Unit tests (NUnit + Moq)                             |

## Requirements

### System Requirements

- **Windows 10/11** (version 22H2 or later)
- **Administrator privileges** for service installation

### Optional Dependencies

| Dependency   | Required For                            | Download                                       |
| ------------ | --------------------------------------- | ---------------------------------------------- |
| **HWiNFO64** | Hardware monitoring (`/stats` endpoint) | [hwinfo.com](https://www.hwinfo.com/download/) |

#### HWiNFO Configuration

To enable hardware monitoring, HWiNFO must be running with shared memory support:

1. Download and install [HWiNFO64](https://www.hwinfo.com/download/)
2. Open HWiNFO and go to **Settings** (gear icon)
3. Navigate to **General / User Interface**
4. Check **Shared Memory Support** ✅
5. Click **OK** and restart HWiNFO in **Sensors-only** mode

> [!TIP]
> Enable "Start minimized" and "Auto Start" in HWiNFO settings for automatic startup with Windows.

Without HWiNFO running, the `/stats` endpoint will return empty sensor data, but all other commands will work normally.

## Getting Started

### Installation

<details open>
<summary><strong>Option 1: MSI Installer (Recommended)</strong></summary>

1. Download the latest `CPCRemote.msi` from [Releases](https://github.com/clindsay94/Connors-PC-Remote/releases)
2. Run the installer as Administrator
3. The installer will:
   - Install to `C:\Program Files\CPCRemote`
   - Register and start the Windows Service automatically
   - Create a Start Menu shortcut

> [!NOTE]
> The MSI installer is GPO-compatible for enterprise deployment.

</details>

<details>
<summary><strong>Option 2: Standalone / Portable</strong></summary>

If you prefer not to use the MSI installer:

1. Download the `publish` folder from the latest release
2. **Right-click `CPCRemote.UI.exe` and select "Run as Administrator"**
3. Navigate to the **Service Management** tab
4. Click **Install Service** (it will automatically find the executable)
5. Configure your IP/Port and click **Start Service**

</details>

<details>
<summary><strong>Option 3: Manual Installation</strong></summary>

You don't even *need* the GUI, the service is all you really need to control your PC from SmartThings (or however you send HTTP requests)

1. Build from source (see [Development](#development))
2. Install the Windows Service:
   ```powershell
   # Run as Administrator
   sc.exe create CPCRemote.Service binPath="C:\Path\To\CPCRemote.Service.exe" start=auto
   ```
3. Reserve the URL for network access:
   ```powershell
   netsh http add urlacl url=http://+:5005/ user=EVERYONE
   ```
4. Start the service:
   ```powershell
   sc.exe start CPCRemote.Service
   ```

</details>

### Configuration

Edit `appsettings.json` in the service directory:

```json
{
	"rsm": {
		"ipAddress": "0.0.0.0",
		"port": 5005,
		"secret": "your-secret-token-here",
		"useHttps": false
	}
}
```

| Option      | Description                                       | Default  |
| ----------- | ------------------------------------------------- | -------- |
| `ipAddress` | IP address to bind (`0.0.0.0` for all interfaces) | Required |
| `port`      | Port number (1-65535)                             | `5005`   |
| `secret`    | Authentication token (empty = no auth)            | `""`     |
| `useHttps`  | Enable HTTPS (coming soon)                        | `false`  |

> [!WARNING]
> Always set a strong secret (8+ characters) when exposing the service to your network.

## Usage

### GUI Application

Launch the CPCRemote.UI application to:

- Install and manage the Windows service
- Configure settings
- Test commands

### HTTP Requests

```bash
# Health check
curl http://localhost:5005/ping

# With authentication
curl -H "Authorization: Bearer your-secret" http://localhost:5005/shutdown

# Alternative URL-based auth
curl http://localhost:5005/your-secret/shutdown
```

## API Reference

### Commands

| Endpoint             | Description                   |
| -------------------- | ----------------------------- |
| `GET /ping`          | Health check (returns 200 OK) |
| `GET /shutdown`      | Graceful shutdown             |
| `GET /restart`       | Restart computer              |
| `GET /lock`          | Lock workstation              |
| `GET /turnscreenoff` | Turn off display              |
| `GET /forceshutdown` | Force shutdown (10s delay)    |
| `GET /uefireboot`    | Reboot to UEFI settings       |
| `GET /apps`          | App catalog (JSON)            |
| `GET /launch/{slot}` | Launch configured app         |

### Response Codes

| Code  | Meaning         |
| ----- | --------------- |
| `200` | Success         |
| `400` | Invalid command |
| `401` | Unauthorized    |
| `500` | Server error    |

## Development

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 17.8+ with:
  - .NET desktop development workload
  - Windows App SDK

### Build

```powershell
# Clone the repository
git clone https://github.com/clindsay94/Connors-PC-Remote.git
cd Connors-PC-Remote

# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test
```

### Build MSI Installer

```powershell
# 1. Publish UI and Service
dotnet publish CPCRemote.UI -c Release -r win-x64 --self-contained -o publish/UI
dotnet publish CPCRemote.Service -c Release -r win-x64 --self-contained -o publish/Service

# 2. Build the MSI
dotnet build CPCRemote.Installer -c Release

# Output: bin/Release/CPCRemote.Installer/CPCRemote.msi
```

### Run Locally

```powershell
# Run the service (requires admin)
dotnet run --project CPCRemote.Service

# Run the UI
dotnet run --project CPCRemote.UI
```

## Troubleshooting

<details>
<summary><strong>Service won't start (Access Denied)</strong></summary>

Reserve the URL for the HTTP listener:

```powershell
netsh http add urlacl url=http://+:5005/ user=EVERYONE
```

</details>

<details>
<summary><strong>Cannot connect to service</strong></summary>

1. Verify service is running: `sc.exe query CPCRemote.Service`
2. Check firewall allows port 5005
3. Verify IP and port in `appsettings.json`
4. Check Windows Event Viewer for errors

</details>

<details>
<summary><strong>401 Unauthorized</strong></summary>

1. Verify secret matches in `appsettings.json`
2. Use header: `Authorization: Bearer your-secret`
3. Check for trailing spaces in the secret

</details>

## Security

> [!CAUTION]
> This application allows remote control of your PC. Take security seriously:

- **Use strong secrets** — Minimum 8 characters, random, unique
- **Limit network exposure** — Bind to `localhost` for local-only access
- **Configure firewall** — Only allow connections from trusted IPs
- **Enable HTTPS** — Use certificate binding for encrypted communication
- **Monitor logs** — Check Windows Event Viewer for unauthorized attempts

## Resources

- [Windows App SDK Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
- [WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery)

## SmartThings Integration

A companion SmartThings Edge driver is available for controlling your PC directly from the SmartThings app. Features include:

- Remote power commands (shutdown, restart, lock, etc.)
- App launcher with slots configurable from the Windows app
- Integration with SmartThings automations and routines

**[Join the SmartThings Channel](https://bestow-regional.api.smartthings.com/invite/Y723VRdPaWMr)** to install the driver.

> [!NOTE]
> This is my first Windows application and SmartThings Edge driver. Things may not work perfectly, and there might be bugs or unexpected behavior. Feedback and patience are appreciated!
