using System.Runtime.Versioning;

using CPCRemote.Core.Helpers;
using CPCRemote.Core.Constants;
using CPCRemote.Core.Interfaces;
using CPCRemote.Core.IPC;
using CPCRemote.Service;
using CPCRemote.Service.Options;
using CPCRemote.Service.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

[assembly: SupportedOSPlatform("windows10.0.22621.0")]

// Ensure configuration files exist in writable location (copies defaults if needed)
string appSettingsPath = ConfigurationPaths.EnsureServiceConfigExists("appsettings.json");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Clear default configuration sources and add our writable path
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(ConfigurationPaths.ServiceDataPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddWindowsService(options => { options.ServiceName = ServiceConstants.RemoteShutdownServiceName; });

builder.Services.AddSingleton<IValidateOptions<RsmOptions>, RsmOptionsValidator>();
builder.Services.Configure<RsmOptions>(builder.Configuration.GetSection("rsm"));
builder.Services.AddOptions<RsmOptions>().Bind(builder.Configuration.GetSection("rsm")).ValidateDataAnnotations().ValidateOnStart();

// Sensor configuration for customizable HWiNFO sensor matching
// Validates on startup to fail fast if configuration is invalid
builder.Services.AddSingleton<IValidateOptions<SensorOptions>, SensorOptionsValidator>();
builder.Services.Configure<SensorOptions>(builder.Configuration.GetSection("sensors"));
builder.Services.AddOptions<SensorOptions>().Bind(builder.Configuration.GetSection("sensors")).ValidateOnStart();

// Core Services
builder.Services.AddSingleton<UserSessionLauncher>();
builder.Services.AddSingleton<AppCatalogService>();
builder.Services.AddSingleton<HardwareMonitor>();

// Named Pipe IPC Server
builder.Services.AddSingleton<NamedPipeServer>();
builder.Services.AddSingleton<IPipeServer>(static sp => sp.GetRequiredService<NamedPipeServer>());

builder.Services.Configure<CPCRemote.Core.Models.WolOptions>(builder.Configuration.GetSection("wol"));
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<CPCRemote.Core.Models.WolOptions>>().Value);

if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<CommandHelper>();
    builder.Services.AddSingleton<ICommandCatalog>(static sp => sp.GetRequiredService<CommandHelper>());
    builder.Services.AddSingleton<ICommandExecutor>(static sp => sp.GetRequiredService<CommandHelper>());
}

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();