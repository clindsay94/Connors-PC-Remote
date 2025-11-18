# Connor's PC Remote

A Windows application for remotely controlling PC power functions via HTTP commands. Built with .NET 10 and WinUI 3.

## Features

- **Remote Power Management**: Shutdown, restart, lock, and more via HTTP requests
- **Windows Service**: Runs as a background service with automatic startup
- **Modern GUI**: WinUI 3 interface for easy service management and configuration
- **Secure Authentication**: Optional Bearer token authentication (header-based)
- **Flexible Configuration**: Configure IP address, port, and security settings
- **Command Support**:
  - Shutdown
  - Restart
  - Lock workstation
  - Turn screen off
  - Force shutdown
  - UEFI firmware reboot

## Architecture

The project consists of four main components:

- **CPCRemote.Core**: Shared library containing command execution logic and interfaces
- **CPCRemote.Service**: Windows Service that listens for HTTP requests
- **CPCRemote.UI**: WinUI 3 application for service management and testing
- **CPCRemote.Tests**: Unit tests for core functionality

## Prerequisites

- Windows 10/11 (version 22H2 or later)
- .NET 10 SDK
- Visual Studio 2022 (for building) or .NET Runtime (for running)
- Administrator privileges (for service installation)

## Installation

### Option 1: MSIX Package (Recommended)

1. Download the latest MSIX package from the releases page
2. Double-click the MSIX file to install
3. Launch "Connor's PC Remote" from the Start menu
4. Follow the in-app setup wizard to install and configure the service

### Option 2: Manual Installation

1. Build the solution in Visual Studio:
   ```
   dotnet build CPCRemote.sln --configuration Release
   ```

2. Install the Windows Service:
   ```powershell
   # Run as Administrator
   sc.exe create CPCRemote.Service binPath="C:\Path\To\CPCRemote.Service.exe" start=auto
   ```

3. Reserve the URL (if binding to non-localhost addresses):
   ```powershell
   # Run as Administrator
   netsh http add urlacl url=http://+:5005/ user=EVERYONE
   ```

4. Start the service:
   ```powershell
   sc.exe start CPCRemote.Service
   ```

## Configuration

Configuration is stored in `appsettings.json` located in the service installation directory.
Note that the ip address of the pc must match the configuration of the app sending the request. 

### Example Configuration

```json
{
  "rsm": {
    "ipAddress": 00.0.0.0, 
    "port": 5005,
    "secret": "your-secret-token-here"
  }
}
```

### Configuration Options

| Option | Description | Default | Required |
|--------|-------------|---------|----------|
| `ipAddress` | IP address to bind to (specific IP) | | | yes |
| `port` | Port number (1-65535) | `5005` | Yes |
| `secret` | Authentication token (min 8 characters, empty = no auth) | `""` | No |

**Security Recommendations:**
- Always set a strong secret (16+ characters) for production use
- Use `localhost` if only accepting local connections
- Use `+` or specific IP for network access (requires URL reservation)
- Consider using HTTPS for encrypted communication (future feature)

## Usage

### Using the GUI

1. Launch the CPCRemote.UI application
2. Navigate to "Service Management"
3. Install and configure the service
4. Test commands using the built-in test interface

### HTTP API

Send HTTP GET requests to control the PC:

```bash
# Without authentication
curl http://localhost:5005/shutdown

# With authentication (Bearer token)
curl -H "Authorization: Bearer your-secret-token" http://localhost:5005/restart
```

### Available Commands

| Command | Description |
|---------|-------------|
| `ping` | Health check (returns 200 OK) |
| `shutdown` | Graceful shutdown |
| `restart` | Restart the computer |
| `lock` | Lock the workstation |
| `turnscreenoff` | Turn off the display |
| `forceshutdown` | Force shutdown (10 second delay) |
| `uefireboot` | Reboot to UEFI firmware settings |

### Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Invalid command |
| 401 | Unauthorized (invalid or missing secret) |
| 500 | Internal server error |

**not every companion app will have any responses displayed. 


## Building from Source

### Requirements

- Visual Studio 2022 (17.8 or later) with:
  - .NET desktop development workload
  - Windows App SDK 1.3 (WinUI 3)
- .NET 10 SDK

### Build Steps

```powershell
# Clone the repository
git clone https://github.com/yourusername/connors-pc-remote.git
cd connors-pc-remote

# Restore dependencies
dotnet restore

# Build all projects
dotnet build --configuration Release

# Run tests
dotnet test

# Package for deployment (MSIX)
msbuild CPCRemote.UI/CPCRemote.UI.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxPackageDir="../publish/" /p:GenerateAppxPackageOnBuild=true
```

## Troubleshooting

### Service Won't Start

**Problem**: Access denied error when starting service

**Solution**: Reserve the URL (requires Administrator):
```powershell
netsh http add urlacl url=http://+:5005/ user=EVERYONE
```

### Cannot Connect to Service

**Problem**: Connection refused or timeout

**Checks**:
1. Verify service is running: `sc.exe query CPCRemote.Service`
2. Check firewall allows connections on the configured port
3. Verify correct IP address and port in configuration
4. Check Windows Event Viewer for service errors

### Authentication Fails

**Problem**: Receiving 401 Unauthorized

**Solution**:
1. Verify secret matches in `appsettings.json`
2. Ensure you're sending the `Authorization: Bearer {token}` header
3. Check for trailing spaces in the secret

### Service Installation Requires Admin

**Problem**: UI shows error when installing service

**Solution**: Run the CPCRemote.UI application as Administrator, or restart when prompted for elevation.

## Security Considerations

⚠️ **IMPORTANT**: This application allows remote control of your PC. Take security seriously:

1. **Use Strong Secrets**: Minimum 16 characters, random, unique
2. **Limit Network Exposure**: Use `localhost` for local-only access
3. **Firewall Rules**: Only allow connections from trusted IP addresses
4. **HTTPS**: Consider using a reverse proxy (nginx, IIS) with HTTPS
5. **Monitor Logs**: Regularly check Windows Event Viewer for unauthorized attempts
6. **Regular Updates**: Keep the application and Windows updated

**Current Limitations:**
- HTTP only (no built-in HTTPS support)
- No rate limiting (add via reverse proxy if needed)
- No request logging to file (only Windows Event Log)

## Development

### Project Structure

```
CPCRemote/
├── CPCRemote.Core/          # Shared library
│   ├── Enums/              # Command type definitions
│   ├── Helpers/            # Command execution logic
│   ├── Interfaces/         # Service interfaces
│   └── Models/             # Data models
├── CPCRemote.Service/       # Windows Service
│   ├── Options/            # Configuration classes
│   ├── Worker.cs           # Background service logic
│   └── Program.cs          # Service host setup
├── CPCRemote.UI/            # WinUI 3 GUI
│   ├── Pages/              # UI pages
│   ├── Helpers/            # UI helpers
│   └── Assets/             # Images and resources
└── CPCRemote.Tests/         # Unit tests
```

### Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style

- Follow C# naming conventions
- Use XML documentation comments for public APIs
- Write unit tests for new features
- Keep methods focused and under 50 lines when possible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

### Version 1.0.1 (Current)

#### Security Enhancements
- ✅ Implemented header-based authentication (Bearer tokens)
- ✅ Removed hardcoded default credentials
- ✅ Added configuration validation with detailed error messages
- ✅ Improved error handling with contextual information

#### Reliability Improvements
- ✅ Added exponential backoff retry logic (max 10 attempts)
- ✅ Enhanced command execution error handling
- ✅ Fixed test project dependencies (NUnit, Moq)

#### Known Issues
- HTTPS support not yet implemented (planned for v1.1.0)
- UI XAML file has minor corruption (service functionality unaffected)

### Version 1.0.0 (Initial Release)

- Basic HTTP listener service
- Power management commands
- WinUI 3 management interface
- Service installation and configuration

## Roadmap

### Planned for v1.1.0
- [ ] HTTPS support with certificate configuration
- [ ] UI XAML fixes and enhancements
- [ ] Administrator elevation prompts in UI
- [ ] Logging to file in addition to Event Log

### Planned for v1.2.0
- [ ] Rate limiting for security
- [ ] Command scheduling
- [ ] Web-based management interface
- [ ] Mobile companion app

## Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Check existing issues for solutions
- Review the troubleshooting section above

## Acknowledgments

- Built with [.NET 8](https://dotnet.microsoft.com/)
- UI powered by [Windows App SDK (WinUI 3)](https://docs.microsoft.com/en-us/windows/apps/winui/)
- Inspired by the need for simple, secure remote PC management

---

**Made with ❤️ for the Windows community**
