using System.Runtime.Versioning;

using CPCRemote.Core.Helpers;
using CPCRemote.Core.Interfaces;
using CPCRemote.Core.IPC;
using CPCRemote.Service;
using CPCRemote.Service.Options;
using CPCRemote.Service.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

[assembly: SupportedOSPlatform("windows10.0.22621.0")]

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => { options.ServiceName = "Remote Shutdown Service"; });

builder.Services.AddSingleton<IValidateOptions<RsmOptions>, RsmOptionsValidator>();
builder.Services.Configure<RsmOptions>(builder.Configuration.GetSection("rsm"));
builder.Services.AddOptions<RsmOptions>().Bind(builder.Configuration.GetSection("rsm")).ValidateDataAnnotations().ValidateOnStart();

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