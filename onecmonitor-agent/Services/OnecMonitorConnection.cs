using OnecMonitor.Common;
using OnecMonitor.Common.DTO;
using MessagePack;
using System.Net.Sockets;
using System.Net;
using OneSTools.Common.Platform;

namespace OnecMonitor.Agent.Services
{
    public class OnecMonitorConnection : ServerConnection
    {
        public OnecMonitorConnection(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime) 
            : base(serviceProvider.GetRequiredService<ILogger<OnecMonitorConnection>>())
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            using var scope = serviceProvider.CreateAsyncScope();
            using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var agentInstance = appDbContext.AgentInstance.FirstOrDefault();

            var instanceName = configuration.GetValue("Agent:InstanceName", Environment.MachineName);
            if (string.IsNullOrEmpty(instanceName))
                instanceName = Environment.MachineName;

            if (agentInstance == null) 
            {
                agentInstance = new AgentInstance
                {
                    Id = Guid.NewGuid(),
                    InstanceName = instanceName,
                    UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds
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
            
            Connected += async (_, _) =>
            {
                await WriteMessageToStream(MessageType.AgentInfo, agentInstance, hostApplicationLifetime.ApplicationStopping);
            };

            var host = configuration.GetValue("OnecMonitor:Host", "0.0.0.0");
            var port = configuration.GetValue("OnecMonitor:Port", 7001);

            var logger = serviceProvider.GetRequiredService<ILogger<OnecMonitorConnection>>();
            
            Start(host, port, logger, hostApplicationLifetime.ApplicationStopping);
        }
    }
}