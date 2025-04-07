using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Systemd;
using OnecMonitor.Agent;
using OnecMonitor.Agent.Models;
using OnecMonitor.Agent.Services;
using OnecMonitor.Agent.Services.EventLog;
using OnecMonitor.Agent.Services.MaintenanceTasks;
using OnecMonitor.Agent.Services.TechLog;
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OnecMonitor.Common.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddWindowsService(options =>
        {
            options.ServiceName = "OnecMonitorAgent";
        });
        services.AddSystemd();

        services.AddSingleton<FilesProvider>();
        
        services.AddSingleton<V8PlatformsProvider>();
        services.AddSingleton<V8ServicesProvider>();
        
        services.AddSingleton<RasHolder>();
        services.AddDbContext<AppDbContext>();
        
        services.AddTransient<OnecMonitorConnection>();

        services.AddSingleton<MonitorQueue<MaintenanceTaskDto>>();
        services.AddHostedService<MaintenanceTaskExecutor>();

        services.AddSingleton<EventLogRepositoryManager>();
        services.AddSingleton<EventLogExporter>();
        services.AddSingleton<EventLogExportManager>();
        
        services.AddSingleton<TechLogRepositoryManager>();
        services.AddSingleton<TechLogExporter>();
        services.AddSingleton<TechLogFoldersManager>();
        services.AddSingleton<TechLogReadersManager>();
        services.AddSingleton<TechLogManager>();
        
        services.AddSingleton<CommandsWatcher>();
    })
.Build();

var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await using var scope = host.Services.CreateAsyncScope();
await using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

// update agent instance info
var configuration = host.Services.GetRequiredService<IConfiguration>();

var agentInstance = appDbContext.AgentInstance.AsNoTracking().FirstOrDefault();

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

host.Services.GetRequiredService<TechLogManager>();

await host.Services.GetRequiredService<CommandsWatcher>()
    .Start(appLifetime.ApplicationStopping);

host.Run();
