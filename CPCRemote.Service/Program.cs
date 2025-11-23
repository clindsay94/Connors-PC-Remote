using System.Runtime.Versioning;

using CPCRemote.Core.Helpers;
using CPCRemote.Core.Interfaces;
using CPCRemote.Service;
using CPCRemote.Service.Options;
using CPCRemote.Service.Services; // <--- Add this

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: SupportedOSPlatform("windows10.0.22621.0")]

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => { options.ServiceName = "Remote Shutdown Service"; });

builder.Services.AddSingleton<IValidateOptions<RsmOptions>, RsmOptionsValidator>();
builder.Services.Configure<RsmOptions>(builder.Configuration.GetSection("rsm"));
builder.Services.AddOptions<RsmOptions>().Bind(builder.Configuration.GetSection("rsm")).ValidateDataAnnotations().ValidateOnStart();

// NEW REGISTRATIONS
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("apps"));
builder.Services.AddSingleton<HardwareMonitor>();

builder.Services.Configure<CPCRemote.Core.Models.WolOptions>(builder.Configuration.GetSection("wol"));
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<CommandHelper>();
    builder.Services.AddSingleton<ICommandCatalog>(static sp => sp.GetRequiredService<CommandHelper>());
    builder.Services.AddSingleton<ICommandExecutor>(static sp => sp.GetRequiredService<CommandHelper>());
}

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();