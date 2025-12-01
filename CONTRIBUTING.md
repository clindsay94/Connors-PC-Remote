# **Contributing to Connor's PC Remote 🎮**

First off, **thank you** for even considering contributing to Connor's PC Remote\! Whether you're here to fix a typo, add a shiny new feature, or optimize the heck out of my spaghetti code, you are awesome. 🚀

This document is your roadmap to success. Follow it, and we'll get along famously. Ignore it, and the CI/CD pipeline might eat your PR.

## **📋 Table of Contents**

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-architecture)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Submitting Changes](#submitting-changes)
- [Reporting Issues](#reporting-issues)
- [Security Vulnerabilities](#security-vulnerabilities)
- [License](#license)
- [Tips](#development-tips)


## **Code of Conduct**

We operate on a "Don't be a jerk" policy. Specifically:

* **Be respectful:** Treat others as you want to be treated.  
* **Constructive feedback only:** Critique the code, not the coder.  
* **Own your mistakes:** We all break the build sometimes. Admit it, fix it, move on.  
* **Show empathy:** We are all humans (presumably) behind these keyboards.  
* **Do NOT break the SmartThings integration:** Seriously. See below. 😉

## **Getting Started**

1. **Fork the Repo:** Click that button in the top right.  
2. **Star the Repo:** It releases dopamine in my brain. 🧠  
3. **Search Issues:** Check if your brilliant idea (or the bug you found) is already listed.  
4. **Read the Manual:** Skim the README.md so you know what we're actually building here.

## **Development Setup**

### **🛠️ Prerequisites**

You're going to need a time machine or the latest bleeding-edge tech:

* **OS:** Windows 10/11 (Version 22H2+)  
* **SDK:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (Yes, we live in the future).  
* **IDE:** Visual Studio 2022 (v17.12+) or **Visual Studio Community 2026 Insiders** (18.3 Preview, Build 11222.16 recommended).  
* **Workloads:**  
  * .NET Desktop Development  
  * Windows App SDK  
* **Privileges:** Admin rights (required for Service testing).  
* **Hardware Monitor:** [HWiNFO64](https://www.hwinfo.com/download/) (Optional, but needed for thermal/sensor features).  
* **Fuel:** Coffee ☕ or Yerba Mate.

### **⚙️ Initial Setup**

Fire up your terminal (PowerShell 7.6 preferred):

\# 1\. Clone your fork  
git clone \[https://github.com/YOUR-USERNAME/Connors-PC-Remote.git\](https://github.com/YOUR-USERNAME/Connors-PC-Remote.git)  
cd Connors-PC-Remote

\# 2\. Add upstream remote (so you stay current)  
git remote add upstream \[https://github.com/clindsay94/Connors-PC-Remote.git\](https://github.com/clindsay94/Connors-PC-Remote.git)

\# 3\. Restore dependencies  
dotnet restore

\# 4\. Build the solution  
dotnet build

\# 5\. Run tests (The moment of truth)  
dotnet test

**Note on HWiNFO:** If you are working on sensor data, ensure "Shared Memory Support" is checked in HWiNFO Settings (General / User Interface).

## **Project Architecture**

CPCRemote/  
├── CPCRemote.Core/      \# 🧠 The Brains: Shared logic, models, interfaces  
├── CPCRemote.Service/   \# ⚙️ The Muscle: Windows Service, HTTP listener, IPC  
├── CPCRemote.UI/        \# 🎨 The Face: WinUI 3 management application  
└── CPCRemote.Tests/     \# 🛡️ The Shield: NUnit \+ Moq tests

## **🛑 CRITICAL: SmartThings Integration Compatibility**

\[\!IMPORTANT\]  
READ THIS CAREFULLY. \> This application powers a SmartThings Edge Driver written in Lua 5.3. The driver is dumb; the PC is smart. If you change the API signature, the driver breaks, and my house stops working.

### **The Golden Rules of the API**

1. **Legacy URLs are Sacred:** Do NOT change existing endpoint paths.  
2. **Verbs stay put:** GET remains GET.  
3. **Auth is immutable:** Do NOT touch the authentication logic without a synchronized driver update.  
4. **Status Codes:** 200 means OK. Don't change it to 201 just to be pedantic.

### **What is Allowed? ✅**

* Adding **NEW** endpoints.  
* Adding **Optional** query parameters.  
* Adding fields to the JSON response (the Lua driver ignores unknown fields).

### **Testing Compatibility**

Before PRs, verify auth works both ways:

\# Header Auth  
Invoke-WebRequest \-Uri "http://ipaddress:5005/shutdown" \-Headers @{"Authorization"="Bearer your-secret"}

\# URL Auth (The SmartThings Way)  
Invoke-WebRequest \-Uri "http://ipaddress:5005/your-secret/shutdown"

## **How to Contribute**

We welcome:

* 🐛 **Bug Fixes:** Especially the obscure ones.  
* ✨ **New Features:** Just don't break the API.  
* 📝 **Docs:** If you find a typo, fix it\!  
* 🧪 **Tests:** You can never have enough code coverage.

### **Workflow**

1. **Find an Issue:** Look for labels like good first issue or help wanted.  
2. **Discuss:** Drop a comment on the issue before you write code so we don't duplicate work.  
3. **Branch:** git checkout \-b feature/super-cool-feature

## **Coding Standards**

* **Language:** C\# 14 / .NET 10 features encouraged.  
* **Style:** Standard .NET conventions. PascalCase for public, \_camelCase for private fields.  
* **Formatting:** 4 spaces. No tabs. *This is not a debate.*  
* **Braces:** Allman style (new line).  
* **Comments:** XML docs for public APIs are mandatory.

/// \<summary\>  
/// Shuts down the computer gracefully.   
/// \</summary\>  
/// \<returns\>True if the shutdown signal was sent.\</returns\>  
public bool Shutdown()  
{  
    // Implementation  
}

## **Testing Guidelines**

We use **NUnit** and **Moq**. If you fix a bug, write a regression test. If you add a feature, write a unit test.

\# Run tests with coverage because we care about quality  
dotnet test /p:CollectCoverage=true

## **Submitting Changes**

### **Commit Messages**

Follow the [Conventional Commits](https://www.conventionalcommits.org/) spec:

* feat(service): add HTTPS support  
* fix(ui): fix crash on startup  
* docs: update readme

### **Pull Request Checklist**

When you open that PR, ensure you've checked these boxes:

* \[ \] 🧪 All tests pass locally.  
* \[ \] 🧹 Code is formatted and warning-free.  
* \[ \] 📜 Documentation updated.  
* \[ \] 🤖 **SmartThings Compatibility Verified** (if touching the API).

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

## **Reporting Issues**

* **Bugs:** Include steps to reproduce, Windows version, and logs.  
* **Security:** 🔒 **DO NOT** open a public issue for security exploits. Email the maintainer directly.  
* **Features:** detailed descriptions and use-cases help us prioritize.

## **License** 

By contributing to Connor's PC Remote, you agree that your contributions will be licensed under the same license as the project.

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

Thanks for helping make Connor's PC Remote better\! Happy Coding\! 💻✨
