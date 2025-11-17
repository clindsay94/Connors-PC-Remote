using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using CPCRemote.Core.Helpers;
using CPCRemote.Core.Interfaces;
using CPCRemote.Service;
using CPCRemote.Service.Options;

// Mark this assembly as Windows-only (minimum Windows 10 build 22621).
// This informs the analyzer that top-level initialization is Windows-only
// and prevents CA1416 warnings for Windows-only APIs used below.
[assembly: SupportedOSPlatform("windows10.0.22621.0")]

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Remote Shutdown Service";
});

// Bind "rsm" section to RsmOptions and register it with DI so Worker can consume typed options.
// Register validator to ensure configuration is valid on startup
builder.Services.AddSingleton<IValidateOptions<RsmOptions>, RsmOptionsValidator>();
builder.Services.Configure<RsmOptions>(builder.Configuration.GetSection("rsm"));
builder.Services.AddOptions<RsmOptions>()
    .Bind(builder.Configuration.GetSection("rsm"))
    .ValidateOnStart();

// Register shared command helper (executes power actions locally on the service
// machine)
// Perform a runtime platform check to avoid registering Windows-only services
// on non-Windows platforms when running the binary outside of Windows.
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<CommandHelper>();
    builder.Services.AddSingleton<ICommandCatalog>(static sp => sp.GetRequiredService<CommandHelper>());
    builder.Services.AddSingleton<ICommandExecutor>(static sp => sp.GetRequiredService<CommandHelper>());
}

builder.Services.AddHostedService<Worker>();

// Build the host from the same HostApplicationBuilder and run it.
// Note: Do not call builder.Host.UseWindowsService() � HostApplicationBuilder doesn't expose a Host property.
IHost host = builder.Build();

host.Run();