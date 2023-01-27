using Microsoft.EntityFrameworkCore;
using OnecMonitor.Agent;
using OnecMonitor.Agent.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddDbContext<AppDbContext>();
        services.AddSingleton<ServerConnection>();
        services.AddSingleton<TechLogFolderWatcher>();
        services.AddSingleton<TechLogExporter>();
        services.AddHostedService<TechLogSeancesWatcher>();
        services.AddHostedService<CommandsWatcher>();
    })
.Build();

using var scope = host.Services.CreateAsyncScope();
using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var techLogeExporter = host.Services.GetRequiredService<TechLogExporter>();
appLifetime.ApplicationStarted.Register(() =>
{
    techLogeExporter.Start();
});
appLifetime.ApplicationStopping.Register(() =>
{
    techLogeExporter.Stop();
    techLogeExporter.Dispose();
});

host.Run();
