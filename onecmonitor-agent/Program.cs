using System.Reflection;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Agent;
using OnecMonitor.Agent.Models;
using OnecMonitor.Agent.Services;
using OnecMonitor.Agent.Services.MaintenanceTasks;
using OnecMonitor.Agent.Services.TechLog;
using OnecMonitor.Common.Storage;
using OnecMonitor.Common.TechLog;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddWindowsService(options =>
        {
            options.ServiceName = "OnecMonitorAgent";
        });
        services.AddSystemd();
        services.AddSingleton<RasHolder>();
        services.AddDbContext<AppDbContext>();
        
        services.AddTransient<OnecMonitorConnection>();
        
        services.AddSingleton<TechLogFolderWatcher>();
        services.AddSingleton<TechLogExporter>();
        services.AddHostedService<TechLogSeancesWatcher>();
        
        services.AddSingleton<MaintenanceTaskExecutorQueue>();
        services.AddHostedService<MaintenanceTaskExecutor>();
        
        services.AddSingleton<CommandsWatcher>();
    })
.Build();

var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await using var scope = host.Services.CreateAsyncScope();
await using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

// update agent instance info
var configuration = host.Services.GetRequiredService<IConfiguration>();

var agentInstance = appDbContext.AgentInstance.FirstOrDefault();

var instanceName = configuration.GetValue("Agent:InstanceName", Environment.MachineName);
if (string.IsNullOrEmpty(instanceName))
    instanceName = Environment.MachineName;

if (agentInstance == null) 
{
    agentInstance = new AgentInstance
    {
        Id = Guid.NewGuid(),
        InstanceName = instanceName
    };

    appDbContext.AgentInstance.Add(agentInstance);
    appDbContext.SaveChanges();
}
else if (agentInstance.InstanceName != instanceName)
{
    agentInstance.InstanceName = instanceName;

    appDbContext.AgentInstance.Update(agentInstance);
    appDbContext.SaveChanges();
}


// another init actions
var techLogExporter = host.Services.GetRequiredService<TechLogExporter>();
appLifetime.ApplicationStopping.Register(() =>
{
    techLogExporter.Stop();
    techLogExporter.Dispose();
});

await host.Services.GetRequiredService<CommandsWatcher>().Start(appLifetime.ApplicationStopping);

host.Run();
