using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using OneSwiss.Agent;
using OneSwiss.Agent.Models;
using OneSwiss.Agent.Oscript;
using OneSwiss.Agent.Services;
using OneSwiss.Agent.Services.EventLog;
using OneSwiss.Agent.Services.MaintenanceTasks;
using OneSwiss.Agent.Services.TechLog;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Services;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddWindowsService(options => { options.ServiceName = "OneSwissAgent"; });
        services.AddSystemd();

        services.AddSingleton<TokenRetriever>();

        services.AddSingleton<FilesProvider>();

        services.AddSingleton<V8PlatformsProvider>();
        services.AddSingleton<V8ServicesProvider>();
        services.AddSingleton<EdtInstallationsProvider>();

        services.AddSingleton<RasHolder>();
        services.AddDbContext<AppDbContext>();

        services.AddSingleton(sp =>
        {
            using var scope = sp.CreateScope();

            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            return CreateAgentInstance(configuration, db);
        });

        services.AddTransient<OneSwissConnection>();
        services.AddKeyedTransient(OneSwissConnection.CommonKey, (sp, _) =>
        {
            var connection = sp.GetRequiredService<OneSwissConnection>();
            connection.Start().Wait();

            return connection;
        });

        services.AddSingleton<MonitorQueue<MaintenanceTaskDto>>();
        services.AddHostedService<MaintenanceTaskExecutor>();
        
        services.AddSingleton<EventLogExporter>();
        services.AddSingleton<MonitorQueue<EventLogSettingsDto>>();
        services.AddHostedService<EventLogExportManager>();

        services.AddSingleton<TechLogRepositoryManager>();
        services.AddSingleton<TechLogExporter>();
        services.AddSingleton<TechLogFoldersManager>();
        services.AddSingleton<TechLogReadersManager>();
        services.AddSingleton<TechLogManager>();

        services.AddSingleton<CommandsWatcher>();
        services.AddSingleton<AgentsResourcesProvider>();

        services.AddSingleton<OscriptIntegrationContext>();
        services.AddSingleton<OscriptIntegrationGlobalContext>();
    })
    .ConfigureLogging(opt =>
    {
        opt.AddSimpleConsole(cfg =>
        {
            cfg.ColorBehavior = LoggerColorBehavior.Enabled;
            cfg.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
    })
    .Build();

var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await using (var scope = host.Services.CreateAsyncScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

host.Services.GetRequiredService<TechLogManager>();

_ = host.Services.GetRequiredService<CommandsWatcher>()
    .Start(appLifetime.ApplicationStopping).ConfigureAwait(false);

host.Run();

return;

static AgentInstance CreateAgentInstance(IConfiguration configuration, AppDbContext appDbContext)
{
    var agentInstance = appDbContext.AgentInstance.AsNoTracking().SingleOrDefault();

    var instanceName = configuration.GetValue("InstanceName", Environment.MachineName);
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

    return agentInstance;
}