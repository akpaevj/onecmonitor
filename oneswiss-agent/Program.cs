using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Systemd;
using OnecMonitor.Agent;
using OneSwiss.Agent;
using OneSwiss.Agent.Models;
using OneSwiss.Agent.Services;
using OneSwiss.Agent.Services.EventLog;
using OneSwiss.Agent.Services.MaintenanceTasks;
using OneSwiss.Agent.Services.TechLog;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Services;

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
        
        services.AddScoped<V8FilesDownloader>();
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

_ = host.Services.GetRequiredService<CommandsWatcher>()
    .Start(appLifetime.ApplicationStopping).ConfigureAwait(false);

host.Run();
