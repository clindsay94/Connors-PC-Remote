# Contributing to Connor's PC Remote

Thank you for your interest in contributing to Connor's PC Remote! This document provides guidelines and instructions for contributing to the project. Whether you're fixing a typo or adding a major feature, we appreciate your help! 🚀

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Submitting Changes](#submitting-changes)
- [Reporting Issues](#reporting-issues)
- [Security Vulnerabilities](#security-vulnerabilities)
- [License](#license)

## Code of Conduct

This project welcomes contributions from everyone. We expect all contributors to:

- Be respectful and considerate in all interactions
- Provide constructive feedback
- Focus on what is best for the community
- Show empathy towards other community members
- Accept responsibility and apologize for mistakes
- Not break the SmartThings integration (see below) 😉

## Getting Started

Before you begin contributing, please:

1. **Fork the repository** to your own GitHub account
2. **Star the project** if you find it useful (it makes me happy!)
3. **Check existing issues** to see if your idea or bug has already been reported
4. **Read the README** to understand the project architecture and features
5. **Remember**: This is my first Windows application, so your patience and constructive feedback are greatly appreciated!

## Development Setup

### Prerequisites

- Windows 10/11 (version 22H2 or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 17.12+ (Microsoft Visual Studio Community 2026 Insiders (18.3 Preview) [Build 11222.16] myself)
- Required workloads:
  - .NET desktop development workload
  - Windows App SDK
- Administrator privileges for testing service functionality
- [HWiNFO64](https://www.hwinfo.com/download/) (optional, for hardware monitoring features)
- Coffee ☕ (highly recommended for debugging sessions)

### Initial Setup

1. **Clone your fork:**
```bash
   git clone https://github.com/YOUR-USERNAME/Connors-PC-Remote.git
   cd Connors-PC-Remote
```

2. **Add upstream remote:**
```bash
   git remote add upstream https://github.com/clindsay94/Connors-PC-Remote.git
```

3. **Restore dependencies:**
```bash
   dotnet restore
```

4. **Build the solution:**
```bash
   dotnet build
```

5. **Run tests to verify setup:**
```bash
   dotnet test
```
   
   If all tests pass, congratulations! If not, welcome to software development. 🎉

### Setting Up HWiNFO (Optional)

If you're working on hardware monitoring features:

1. Install HWiNFO64
2. Open HWiNFO and go to Settings (gear icon)
3. Navigate to General / User Interface
4. Check "Shared Memory Support" ✅
5. Restart HWiNFO in Sensors-only mode

## Project Structure
```
CPCRemote/
├── CPCRemote.Core/          # Shared library (commands, models, interfaces)
├── CPCRemote.Service/       # Windows Service (HTTP listener, IPC server)
├── CPCRemote.UI/            # WinUI 3 management application
└── CPCRemote.Tests/         # Unit tests (NUnit + Moq)
```

### Component Responsibilities

- **CPCRemote.Core**: Shared logic, models, and interfaces used across projects
- **CPCRemote.Service**: Background service that handles HTTP requests and executes system commands
- **CPCRemote.UI**: Desktop application for configuring and managing the service
- **CPCRemote.Tests**: Automated tests for all components (because we're responsible developers)

## How to Contribute

### Types of Contributions

We welcome various types of contributions:

- 🐛 **Bug fixes** (especially the weird ones)
- ✨ **New features** (that don't break SmartThings integration!)
- 📝 **Documentation improvements** (clarity is king)
- 🧪 **Test coverage enhancements** (more tests = more confidence)
- 🎨 **UI/UX improvements** (make it pretty AND functional)
- 🔒 **Security enhancements** (we take this seriously)
- ♿ **Accessibility improvements**
- 🌍 **Localization/translations**

### Finding Something to Work On

- Check the [Issues](https://github.com/clindsay94/Connors-PC-Remote/issues) page for open bugs and feature requests
- Look for issues labeled `good first issue` if you're new to the project
- Issues labeled `help wanted` are specifically looking for contributors
- If you have a new idea, open an issue first to discuss it (let's chat before you spend hours coding!)

### Before You Start

1. **Comment on the issue** you'd like to work on to avoid duplicate efforts
2. **Create a new branch** from `main` for your work:
```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/issue-description
```

## ⚠️ CRITICAL: SmartThings Integration Compatibility

This application is designed to work seamlessly with a **SmartThings Edge Driver (written in Lua 5.3)** that sends HTTP requests to control the PC. Any changes to the HTTP API **must maintain backward compatibility** with the SmartThings driver.

### API Compatibility Rules

When modifying the service or API:

1. **Do NOT change existing endpoint URLs** without updating the SmartThings driver
2. **Do NOT change the HTTP method** for existing endpoints (GET remains GET)
3. **Do NOT modify the authentication mechanism** without driver updates
4. **Do NOT change response codes** for existing success/failure scenarios
5. **Do NOT remove or rename existing endpoints** without proper deprecation

### What You CAN Do

- ✅ Add NEW endpoints (they won't break existing functionality)
- ✅ Add optional query parameters to existing endpoints
- ✅ Enhance response bodies (add new fields, but don't remove existing ones)
- ✅ Improve error messages (as long as status codes remain the same)
- ✅ Add new features that don't affect the HTTP API

### Testing SmartThings Compatibility

Before submitting changes that affect the HTTP API:

1. **Test all existing endpoints** with curl/PowerShell
2. **Verify authentication** still works both ways:
   - Header: `Authorization: Bearer your-secret`
   - URL: `http://localhost:5005/your-secret/command`
3. **Check response codes** match expected values (200, 400, 401, 500)
4. **Document any API changes** clearly in your PR

If you're unsure whether your changes might affect SmartThings compatibility, **please ask in the issue or PR discussion** before implementing. Breaking the SmartThings driver means breaking the primary use case for this application! 😱

## Coding Standards

### General Guidelines

- Follow C# coding conventions and .NET naming guidelines
- Use meaningful variable and method names (no `var x = 123;` unless it's obvious)
- Keep methods focused and concise (Single Responsibility Principle)
- Add XML documentation comments for public APIs
- Handle exceptions appropriately (try-catch is your friend)
- Log important operations and errors (future you will thank present you)

### Code Style

- **Indentation**: 4 spaces (no tabs, let's not start that war)
- **Line length**: Aim for 120 characters maximum
- **Braces**: Use Allman style (braces on new lines)
- **Naming conventions**:
  - `PascalCase` for classes, methods, properties
  - `camelCase` for local variables and parameters
  - `_camelCase` for private fields
  - `UPPER_CASE` for constants

### Example
```csharp
namespace CPCRemote.Core.Commands
{
    /// <summary>
    /// Executes system power commands.
    /// </summary>
    public class PowerCommand : ICommand
    {
        private readonly ILogger _logger;
        private const int SHUTDOWN_TIMEOUT = 10;

        public PowerCommand(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Shuts down the computer gracefully.
        /// </summary>
        /// <returns>True if successful, otherwise false.</returns>
        public bool Shutdown()
        {
            try
            {
                _logger.LogInformation("Initiating system shutdown");
                // Implementation
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to shutdown system");
                return false;
            }
        }
    }
}
```

## Testing Guidelines

All code contributions should include appropriate tests. No, really. Please write tests. Future maintainers will appreciate it! 🙏

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test CPCRemote.Tests/CPCRemote.Tests.csproj

# Run a specific test (when debugging that ONE annoying test)
dotnet test --filter "FullyQualifiedName~YourTestName"
```

### Writing Tests

- Use NUnit framework and Moq for mocking
- Follow Arrange-Act-Assert (AAA) pattern
- Use descriptive test method names: `MethodName_StateUnderTest_ExpectedBehavior`
- Test both success and failure scenarios (pessimism pays off here)
- Mock external dependencies
- Test API endpoints to ensure SmartThings compatibility

### Example Test
```csharp
[TestFixture]
public class PowerCommandTests
{
    private Mock<ILogger> _mockLogger;
    private PowerCommand _powerCommand;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _powerCommand = new PowerCommand(_mockLogger.Object);
    }

    [Test]
    public void Shutdown_ValidRequest_ReturnsTrue()
    {
        // Arrange
        // (Setup done in SetUp method)

        // Act
        var result = _powerCommand.Shutdown();

        // Assert
        Assert.IsTrue(result);
        _mockLogger.Verify(x => x.LogInformation(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void Shutdown_ThrowsException_ReturnsFalse()
    {
        // Arrange
        // Setup to throw exception

        // Act
        var result = _powerCommand.Shutdown();

        // Assert
        Assert.IsFalse(result);
        _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }
}
```

### Testing API Changes

If you're modifying the HTTP API, add integration tests to verify:
```csharp
[TestFixture]
public class ApiCompatibilityTests
{
    [Test]
    public void ShutdownEndpoint_WithBearerAuth_Returns200()
    {
        // Ensure SmartThings driver authentication still works
    }

    [Test]
    public void ShutdownEndpoint_WithUrlAuth_Returns200()
    {
        // Ensure alternative auth method still works
    }
}
```

## Submitting Changes

### Commit Messages

Write clear, concise commit messages following this format:
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(service): add HTTPS certificate binding support

Implement certificate binding for HTTPS connections with configurable
certificate selection and validation.

Closes #42
```
```
fix(ui): resolve service status polling issue

Fixed issue where service status would not update correctly after
service restart. Added proper error handling for IPC communication.

Fixes #38
```
```
fix(api): correct endpoint typo without breaking SmartThings

Changed internal routing logic but maintained exact endpoint URLs
to preserve SmartThings Edge Driver compatibility.

Closes #55
```

### Pull Request Process

1. **Update your fork** with the latest changes from upstream:
```bash
   git fetch upstream
   git rebase upstream/main
```

2. **Push your changes** to your fork:
```bash
   git push origin feature/your-feature-name
```

3. **Create a Pull Request** from your fork to the main repository:
   - Use a clear, descriptive title
   - Reference any related issues (e.g., "Closes #123")
   - Provide a detailed description of your changes
   - Include screenshots for UI changes
   - List any breaking changes (especially API changes!)
   - Confirm SmartThings compatibility if applicable

4. **PR Description Template:**
```markdown
   ## Description
   Brief description of what this PR does

   ## Related Issues
   Closes #(issue number)

   ## Changes Made
   - Change 1
   - Change 2
   - Change 3

   ## Testing
   Describe how you tested these changes

   ## SmartThings Compatibility
   - [ ] No API changes (or)
   - [ ] API changes are backward compatible
   - [ ] Tested existing endpoints still work
   - [ ] New endpoints documented

   ## Screenshots (if applicable)
   Add screenshots for UI changes

   ## Checklist
   - [ ] Code follows project style guidelines
   - [ ] Tests have been added/updated
   - [ ] Documentation has been updated
   - [ ] All tests pass locally
   - [ ] No new compiler warnings
   - [ ] SmartThings integration verified (if API changed)
```

5. **Wait for review** and address any feedback (I promise to be nice!)
6. **Once approved**, your PR will be merged!

### PR Requirements

Before submitting, ensure:

- ✅ All tests pass
- ✅ No compiler warnings
- ✅ Code follows style guidelines
- ✅ Documentation is updated
- ✅ Commit messages are clear
- ✅ PR description is complete
- ✅ SmartThings compatibility maintained (if applicable)

## Reporting Issues

### Bug Reports

When reporting bugs, please include:

- **Clear title** describing the issue
- **Description** of what happened vs. what you expected
- **Steps to reproduce** the issue (the more detailed, the better)
- **Environment details**:
  - Windows version
  - .NET version
  - Application version
  - Visual Studio version (if relevant)
- **Logs or error messages** (check Windows Event Viewer)
- **Screenshots** if applicable
- **Severity**: Is this a "can't use the app" bug or a "this button is the wrong color" bug?

### Feature Requests

When suggesting features:

- **Describe the problem** the feature would solve
- **Propose a solution** with details
- **Consider alternatives** you've thought about
- **Additional context** like mockups or examples
- **SmartThings impact**: Will this require changes to the Edge driver?

### Issue Template
```markdown
## Description
Clear description of the issue or feature

## Steps to Reproduce (for bugs)
1. Step 1
2. Step 2
3. Step 3
4. 💥 Kaboom

## Expected Behavior
What you expected to happen

## Actual Behavior
What actually happened

## Environment
- Windows Version: 
- .NET Version: 
- Application Version: 
- Visual Studio Version: 

## SmartThings Related
- [ ] This affects SmartThings integration

## Additional Context
Any other relevant information
```

## Security Vulnerabilities

**Do not report security vulnerabilities through public issues.**

If you discover a security vulnerability:

1. **Do not create a public issue** (seriously, don't)
2. **Email the maintainer** with details
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

We take security seriously and will respond promptly to vulnerability reports. Remote PC control is powerful, and we want to keep it secure! 🔒

## Development Tips

### Debugging the Service

To debug the Windows Service:

1. Build the solution in Debug mode
2. Run Visual Studio as Administrator (right-click, "Run as administrator")
3. Attach to the `CPCRemote.Service.exe` process
4. Or use the UI project to launch and control the service
5. Set breakpoints and start debugging!

### Testing HTTP Endpoints

Use curl or PowerShell to test endpoints:
```bash
# PowerShell
Invoke-WebRequest -Uri "http://localhost:5005/ping" -Method Get

# With authentication
Invoke-WebRequest -Uri "http://localhost:5005/shutdown" -Headers @{"Authorization"="Bearer your-secret"}

# curl (if you're into that)
curl -H "Authorization: Bearer your-secret" http://localhost:5005/shutdown

# Alternative URL-based auth (SmartThings compatible)
curl http://localhost:5005/your-secret/shutdown
```

### Simulating SmartThings Requests

To test like the SmartThings driver would:
```lua
-- This is what the Lua 5.3 SmartThings driver sends
-- Simulate it with:
curl -X GET "http://YOUR-PC-IP:5005/your-secret/shutdown"
```

### Working with IPC

The service and UI communicate via Named Pipes. When debugging IPC issues:

- Ensure only one instance of the service is running
- Check pipe permissions
- Use Process Explorer to verify named pipe creation
- Remember: Named pipes are like tubes, but for data instead of hamsters

### Common Development Tasks
```bash
# Clean solution (when things get weird)
dotnet clean

# Rebuild (turn it off and on again, but for code)
dotnet build --no-incremental

# Run with specific configuration
dotnet run --project CPCRemote.Service --configuration Release

# Create NuGet package (if you're feeling fancy)
dotnet pack
```

## Additional Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Windows Services Documentation](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/)
- [NUnit Documentation](https://docs.nunit.org/)
- [SmartThings Edge Driver Documentation](https://developer.smartthings.com/docs/edge-device-drivers/)
- [Lua 5.3 Reference Manual](https://www.lua.org/manual/5.3/)
- [Stack Overflow](https://stackoverflow.com/) (when all else fails)

## Questions?

If you have questions:

- Check existing issues and discussions
- Open a new issue with the `question` label
- Be patient and respectful when asking for help
- Remember: There are no stupid questions, only questions that haven't been asked yet!

## License

By contributing to Connor's PC Remote, you agree that your contributions will be licensed under the same license as the project.

---

Thank you for contributing to Connor's PC Remote! Your efforts help make this project better for everyone. Whether you're fixing a typo, adding a feature, or just stopping by to say hi, we appreciate you! 🎉

P.S. If you find any bugs, remember: they're not bugs, they're "undocumented features." 😄
