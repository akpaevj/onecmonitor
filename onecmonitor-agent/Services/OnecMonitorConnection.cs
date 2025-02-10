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
        private readonly ILogger<OnecMonitorConnection> _logger;

        public OnecMonitorConnection(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
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
            
            var agentInstance1 = agentInstance;
            
            Connected += async (s, e) =>
            {
                await WriteMessageToStream(MessageType.AgentInfo, agentInstance1, hostApplicationLifetime.ApplicationStopping);
            };

            var host = configuration.GetValue("OnecMonitor:Host", "0.0.0.0");
            var port = configuration.GetValue("OnecMonitor:Port", 7001);

            _logger = serviceProvider.GetRequiredService<ILogger<OnecMonitorConnection>>();
            
            Start(host, port, _logger, hostApplicationLifetime.ApplicationStopping);
        }

        public async Task SubscribeForCommands(CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.SubscribingForCommands, cancellationToken);

            _logger.LogTrace("SubscribingForCommands message is queued");
        }

        public async Task<long> GetLastFilePosition(Guid seanceId, Guid templateId, string folder, string file, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Last position in file requested");
            
            return await WriteMessageAndWaitResult<LastFilePositionRequestDto, long>(
                MessageType.LastFilePositionRequest, 
                MessageType.LastFilePosition,
                new LastFilePositionRequestDto()
                {
                    SeanceId = seanceId,
                    TemplateId = templateId,
                    Folder = folder,
                    File = file
                }, 
                cancellationToken);
        }

        public async Task<List<TechLogSeanceDto>> GetTechLogSeances(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Tech log seances requested");

            return await WriteMessageAndWaitResult<List<TechLogSeanceDto>>(
                MessageType.TechLogSeancesRequest, 
                MessageType.TechLogSeances, 
                cancellationToken);
        }
        
        public async Task SendInstalledPlatforms(Message message, List<V8Platform> platforms, CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.InstalledPlatforms, platforms, message,
                cancellationToken);
        }
        
        public async Task SendV8Services(Message message, List<V8Service> services, CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.V8Services, services, message,
                cancellationToken);
        }
        
        public async Task SendV8Clusters(Message message, List<V8Cluster> clusters, CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.ClustersResponse, clusters, message,
                cancellationToken);
        }
        
        public async Task SendV8InfoBases(Message message, List<V8InfoBaseSummary> infoBases, CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.InfoBasesResponse, infoBases, message,
                cancellationToken);
        }

        public async Task SendTechLogEventContent(TechLogEventContentDto item, CancellationToken cancellationToken)
        {
            await WriteMessage(MessageType.TechLogEventContent, item, null, cancellationToken);
            _logger.LogTrace("Event content message is queued");
        }
    }
}